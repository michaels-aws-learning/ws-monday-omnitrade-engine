using System;
using System.Collections.Generic;
using System.IO;
using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.Route53;
using Constructs;
using EcsSecret = Amazon.CDK.AWS.ECS.Secret;
using ElbHealthCheck = Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck;
using ImagePlatform = Amazon.CDK.AWS.Ecr.Assets.Platform_;
using SmSecret = Amazon.CDK.AWS.SecretsManager.Secret;
using SmSecretProps = Amazon.CDK.AWS.SecretsManager.SecretProps;
using SecretStringGenerator = Amazon.CDK.AWS.SecretsManager.SecretStringGenerator;

namespace Infra
{
    public class EcsStackProps : StackProps
    {
        /// <summary>Route 53 hosted zone that already exists, e.g. "example.com".</summary>
        public string HostedZoneName { get; set; }

        /// <summary>Public API hostname served by the ALB, e.g. "api.example.com".</summary>
        public string ApiDomainName { get; set; }

        /// <summary>Origin of the blotter UI for CORS, e.g. "https://app.example.com". Optional.</summary>
        public string BlotterOrigin { get; set; }

        /// <summary>Docker build context relative to the CDK app's working directory (the folder with cdk.json).</summary>
        public string DockerContextPath { get; set; } = "..";

        public bool PaperTrading { get; set; } = true;
    }

    public class ECSStack : EnvironmentStack
    {
        public Vpc Vpc { get; }
        public ApplicationLoadBalancedFargateService EngineService { get; }

        internal ECSStack(Construct scope, string id, string envName, EcsStackProps props)
            : base(scope, id, envName, props)
        {
            if (string.IsNullOrWhiteSpace(props?.HostedZoneName) || string.IsNullOrWhiteSpace(props.ApiDomainName))
            {
                throw new ArgumentException("EcsStackProps.HostedZoneName and ApiDomainName are required.");
            }

            // ---------- networking ----------
            // Public subnets only: the task gets a public IP for outbound calls (Alpaca, Auth0, ECR)
            // without NAT gateway charges. Inbound is locked to the ALB by the service security group.
            Vpc = new Vpc(this, "Vpc", new VpcProps
            {
                MaxAzs = 2,
                NatGateways = 0,
                SubnetConfiguration = new[]
                {
                    new SubnetConfiguration { Name = "public", SubnetType = SubnetType.PUBLIC, CidrMask = 24 }
                }
            });

            var cluster = new Cluster(this, "Cluster", new ClusterProps { Vpc = Vpc });

            // ---------- DNS and TLS ----------
            // FromLookup requires the stack env (account + region) to be set explicitly.
            var zone = HostedZone.FromLookup(this, "Zone", new HostedZoneProviderProps
            {
                DomainName = props.HostedZoneName
            });

            var certificate = new Certificate(this, "ApiCertificate", new CertificateProps
            {
                DomainName = props.ApiDomainName,
                Validation = CertificateValidation.FromDns(zone)
            });

            // ---------- secrets and logs ----------
            // Created with placeholder values. Put the real Alpaca keys in after the first deploy.
            var alpacaSecret = new SmSecret(this, "AlpacaSecret", new SmSecretProps
            {
                SecretName = $"omnitrade/{envName}/alpaca",
                Description = $"Alpaca API keys for OmniTrade.Engine ({envName})",
                GenerateSecretString = new SecretStringGenerator
                {
                    SecretStringTemplate = "{\"keyId\":\"REPLACE_ME\"}",
                    GenerateStringKey = "secretKey"
                }
            });

            var logGroup = new LogGroup(this, "EngineLogs", new LogGroupProps
            {
                LogGroupName = $"/omnitrade/{envName}/engine",
                Retention = RetentionDays.ONE_MONTH,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            var environment = new Dictionary<string, string>
            {
                // The ALB terminates TLS; this makes ASP.NET Core honor X-Forwarded-Proto/For
                ["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true",
                ["Alpaca__Paper"] = props.PaperTrading ? "true" : "false",
                ["OmniTrade__Environment"] = envName
            };
            if (!string.IsNullOrWhiteSpace(props.BlotterOrigin))
            {
                environment["Cors__AllowedOrigins__0"] = props.BlotterOrigin;
            }

            // ---------- service ----------
            var dockerContext = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), props.DockerContextPath));

            EngineService = new ApplicationLoadBalancedFargateService(this, "Engine", new ApplicationLoadBalancedFargateServiceProps
            {
                Cluster = cluster,
                ServiceName = $"omnitrade-engine-{envName}",
                Cpu = 512,
                MemoryLimitMiB = 1024,
                RuntimePlatform = new RuntimePlatform
                {
                    CpuArchitecture = CpuArchitecture.ARM64,
                    OperatingSystemFamily = OperatingSystemFamily.LINUX
                },

                // Exactly one task owns the broker connection and order state.
                // 0/100 stops the old task before the new one starts, so two never run at once.
                DesiredCount = 1,
                MinHealthyPercent = 0,
                MaxHealthyPercent = 100,
                CircuitBreaker = new DeploymentCircuitBreaker { Rollback = true },
                HealthCheckGracePeriod = Duration.Seconds(60),
                EnableExecuteCommand = true,

                AssignPublicIp = true,
                TaskSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },

                // HTTPS listener with the ACM cert, HTTP -> HTTPS redirect, and a Route 53 alias record
                PublicLoadBalancer = true,
                Protocol = ApplicationProtocol.HTTPS,
                Certificate = certificate,
                SslPolicy = SslPolicy.RECOMMENDED_TLS,
                RedirectHTTP = true,
                DomainName = props.ApiDomainName,
                DomainZone = zone,

                TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
                {
                    ContainerName = "engine",
                    ContainerPort = 8080,
                    Image = ContainerImage.FromAsset(dockerContext, new AssetImageProps
                    {
                        Platform = ImagePlatform.LINUX_ARM64
                    }),
                    Environment = environment,
                    Secrets = new Dictionary<string, EcsSecret>
                    {
                        ["Alpaca__KeyId"] = EcsSecret.FromSecretsManager(alpacaSecret, "keyId"),
                        ["Alpaca__SecretKey"] = EcsSecret.FromSecretsManager(alpacaSecret, "secretKey")
                    },
                    LogDriver = LogDriver.AwsLogs(new AwsLogDriverProps
                    {
                        StreamPrefix = "engine",
                        LogGroup = logGroup
                    })
                }
            });

            // ---------- load balancer tuning ----------
            EngineService.TargetGroup.ConfigureHealthCheck(new ElbHealthCheck
            {
                Path = "/health",
                HealthyHttpCodes = "200",
                Interval = Duration.Seconds(15),
                HealthyThresholdCount = 2,
                UnhealthyThresholdCount = 3
            });
            EngineService.TargetGroup.SetAttribute("deregistration_delay.timeout_seconds", "15");

            // Longer idle timeout for SignalR WebSocket connections (keepalive pings every 15s by default)
            EngineService.LoadBalancer.SetAttribute("idle_timeout.timeout_seconds", "120");

            // ---------- monitoring ----------
            // Fires when no healthy engine task is behind the ALB. Add an SNS action to get notified.
            new Alarm(this, "EngineUnavailable", new AlarmProps
            {
                AlarmName = $"omnitrade-{envName}-engine-unavailable",
                AlarmDescription = "No healthy OmniTrade.Engine task behind the load balancer",
                Metric = EngineService.TargetGroup.Metrics.HealthyHostCount(new MetricOptions
                {
                    Period = Duration.Minutes(1),
                    Statistic = "Minimum"
                }),
                Threshold = 1,
                EvaluationPeriods = 3,
                ComparisonOperator = ComparisonOperator.LESS_THAN_THRESHOLD,
                TreatMissingData = TreatMissingData.BREACHING
            });

            // ---------- outputs ----------
            new CfnOutput(this, "ApiUrl", new CfnOutputProps { Value = $"https://{props.ApiDomainName}" });
            new CfnOutput(this, "HubUrl", new CfnOutputProps { Value = $"wss://{props.ApiDomainName}/hubs/blotter" });
            new CfnOutput(this, "AlpacaSecretName", new CfnOutputProps { Value = alpacaSecret.SecretName });
        }
    }
}
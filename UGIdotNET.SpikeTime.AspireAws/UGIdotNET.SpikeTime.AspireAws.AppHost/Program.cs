using Amazon.CDK.AWS.StepFunctions;

var builder = DistributedApplication.CreateBuilder(args);

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("albx-aspire")
    .WithRegion(Amazon.RegionEndpoint.EUWest1);

var awsStack = builder.AddAWSCDKStack("spiketime-aspire")
    .WithReference(awsConfig);

var s3Bucket = awsStack.AddS3Bucket("spiketime-bucket");

var sqlServer = builder.AddSqlServer("spiketime-sql");

var db = sqlServer.AddDatabase("spiketime-db");

var apiService = builder.AddProject<Projects.UGIdotNET_SpikeTime_AspireAws_ApiService>("apiservice")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(awsConfig)
    .WaitFor(s3Bucket)
    .WithReference(s3Bucket);

builder.AddProject<Projects.UGIdotNET_SpikeTime_AspireAws_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.UGIdotNET_SpikeTime_AspireAws_Migrations>("migrations")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();

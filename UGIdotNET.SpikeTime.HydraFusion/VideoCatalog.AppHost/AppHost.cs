var builder = DistributedApplication.CreateBuilder(args);

var keycloakAdminUsername = builder.AddParameter("keycloak-admin-username", "admin");
var keycloakAdminPassword = builder.AddParameter("keycloak-admin-password", secret: true);
var keycloakClientSecret = builder.AddParameter("keycloak-client-secret", secret: true);
var keycloakDevUserPassword = builder.AddParameter("keycloak-dev-user-password", secret: true);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("videocatalog-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase("videocatalogdb");

var keycloak = builder.AddKeycloak(
        "keycloak",
        port: 8080,
        adminUsername: keycloakAdminUsername,
        adminPassword: keycloakAdminPassword)
    .WithDataVolume("videocatalog-keycloak-data")
    .WithRealmImport("Keycloak")
    .WithEnvironment("ADMIN_PORTAL_CLIENT_SECRET", keycloakClientSecret)
    .WithEnvironment("VIDEO_ADMIN_PASSWORD", keycloakDevUserPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithExternalHttpEndpoints();

var publicApi = builder.AddProject<Projects.VideoCatalog_PublicApi>("publicapi")
    .WithReference(db)
    .WaitFor(db);

var adminApi = builder.AddProject<Projects.VideoCatalog_AdminApi>("adminapi")
    .WithReference(db)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WaitFor(db);

var adminPortal = builder.AddProject<Projects.VideoCatalog_AdminPortal>("adminportal")
    .WithReference(adminApi)
    .WithReference(keycloak)
    .WithEnvironment("AdminApiUrl", adminApi.GetEndpoint("http"))
    .WithEnvironment("Authentication__ClientSecret", keycloakClientSecret)
    .WaitFor(keycloak)
    .WaitFor(adminApi);

var publicPortal = builder.AddNpmApp("publicportal", "../VideoCatalog.PublicPortal")
    .WithReference(publicApi)
    .WithEnvironment("PUBLIC_API_BASE_URL", publicApi.GetEndpoint("http"))
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 4321)
    .WithExternalHttpEndpoints()
    .WaitFor(publicApi);

builder.Build().Run();

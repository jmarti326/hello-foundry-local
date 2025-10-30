var builder = DistributedApplication.CreateBuilder(args);

// Add the web application
var webApp = builder.AddProject<Projects.HelloFoundry_Web>("webfrontend");

builder.Build().Run();
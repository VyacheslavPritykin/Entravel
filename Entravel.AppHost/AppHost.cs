var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", "postgres", secret: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithLifetime(ContainerLifetime.Persistent);

var orderDb = postgres.AddDatabase("orderdb");

var rabbitPassword = builder.AddParameter("rabbitmq-password", "guest", secret: true);

var messaging = builder.AddRabbitMQ("messaging", password: rabbitPassword)
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

var seq = builder.AddSeq("seq")
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.OrderProcessing_API>("order-processing-api")
    .WithReference(orderDb).WaitFor(orderDb)
    .WithReference(messaging).WaitFor(messaging)
    .WithReference(seq).WaitFor(seq);

builder.Build().Run();
using Entities;
using Microsoft.Data.SqlClient;

Console.WriteLine("Hello, World!");


var session = new DbSession(null!, new SqlConnection { ConnectionString = "Server=localhost;Database=Nitro50Acl;Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False" });



//Console.WriteLine(session.Users.Insert("Id, Name", $"{30},{"Lex"}").Then().Insert("Id, Name", $"{31},{"Falco"}").Run());


Console.WriteLine(session.Users.Delete($"Name = {"Falco"}").Run());



Console.WriteLine(session.Run<int>($"select {Dbo.FN("GetAdministratorCount")}()"));

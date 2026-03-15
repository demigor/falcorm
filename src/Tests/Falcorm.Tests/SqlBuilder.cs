using Microsoft.Data.SqlClient;

namespace Istarion.Falcorm.Tests;

using Entities;

public class SqlBuilder
{
  [Test]
  public async Task Build_SimpleSelect()
  {
    var s = new DbSession(null!, new SqlConnection());


    var query = s.Users.Builder();


    await Assert.That(query.Sql).IsEqualTo("select * from dbo.[TUsers]");
  }

  [Test]
  public async Task Build_SimpleSelectAlias()
  {
    var s = new DbSession(null!, new SqlConnection());


    var query = s.Users.Builder().Alias("x");


    await Assert.That(query.Sql).IsEqualTo("select x.* from dbo.[TUsers] x");
  }

  [Test]
  public async Task Build_Select()
  {
    var s = new DbSession(null!, new SqlConnection());


    var query = s.Users.Builder().Select("Name").Select("Id").Select($"{0} as Prio");


    await Assert.That(query.Sql).IsEqualTo("select Name, Id, @p0 as Prio from dbo.[TUsers]");
  }


  [Test]
  public async Task Build_SimpleTableSelect()
  {
    var s = new DbSession(null!, new SqlConnection());


    var query = s.Users.Builder().Table("Users");


    await Assert.That(query.Sql).IsEqualTo("select * from dbo.[Users]");
  }

  [Test]
  public async Task Build_SimpleWhere()
  {
    var s = new DbSession(null!, new SqlConnection());


    var query = s.Users.Where($"{Dbo.CN("Name")} like {"%test%"}");


    await Assert.That(query.Sql).IsEqualTo("select * from dbo.[TUsers] where [Name] like @p0");
  }

  [Test]
  public async Task Build_SimpleWhere2()
  {
    var s = new DbSession(null!, new SqlConnection());


    var query = s.Users.Where($"Name like '%test%'");


    await Assert.That(query.Sql).IsEqualTo("select * from dbo.[TUsers] where Name like '%test%'");
  }

  [Test]
  public async Task Build_SimpleOrderBy()
  {
    var s = new DbSession(null!, new SqlConnection());


    var query = s.Users.OrderBy($"{Dbo.CN("Name")}, {Dbo.CN("Id")} desc");


    await Assert.That(query.Sql).IsEqualTo("select * from dbo.[TUsers] order by [Name], [Id] desc");
  }

  [Test]
  public async Task Build_SimpleGroupBy()
  {
    var s = new DbSession(null!, new SqlConnection());


    var query = s.Users.GroupBy($"{Dbo.CN("Xid")}, {Dbo.CN("Id")}");


    await Assert.That(query.Sql).IsEqualTo("select * from dbo.[TUsers] group by [Xid], [Id]");
  }

  [Test]
  public async Task Build_Limit()
  {
    var s = new DbSession(null!, new SqlConnection());


    var query = s.Users.Take(10);


    await Assert.That(query.Sql).IsEqualTo("select top(@p0) * from dbo.[TUsers]");
  }

  [Test]
  public async Task Build_Paging()
  {
    var s = new DbSession(null!, new SqlConnection());


    var query = s.Users.Skip(1000).Take(100);


    await Assert.That(query.Sql).IsEqualTo("select * from dbo.[TUsers] order by (select 1) offset @p0 rows fetch next @p1 rows only");
  }

  [Test]
  public async Task Build_Paging2()
  {
    var s = new DbSession(null!, new SqlConnection());


    var query = s.Users.Skip(1000).Take(100).Take(40);


    await Assert.That(query.Sql).IsEqualTo("select * from dbo.[TUsers] order by (select 1) offset @p0 rows fetch next @p1 rows only");
  }

  [Test]
  public async Task Build_Paging2_OrderBy()
  {
    var s = new DbSession(null!, new SqlConnection());


    var query = s.Users.Skip(1000).Take(100).OrderBy("Name");


    await Assert.That(query.Sql).IsEqualTo("select * from (select * from dbo.[TUsers] order by (select 1) offset @p0 rows fetch next @p1 rows only) order by Name");
  }

  [Test]
  public async Task Build_Insert()
  {
    var s = new DbSession(null!, new SqlConnection());
    var query = s.Users.Insert("Id, Name", $"{1},{"a"}");
    await Assert.That(query.Sql).IsEqualTo("insert into dbo.[TUsers] (Id, Name) values (@p0,@p1)");
  }

  [Test]
  public async Task Build_Update()
  {
    var s = new DbSession(null!, new SqlConnection());
    var query = s.Users.Update($"Name = {"x"}");
    await Assert.That(query.Sql).IsEqualTo("update t set Name = @p0 from dbo.[TUsers] t");
  }

  [Test]
  public async Task Build_Delete_NoWhere()
  {
    var s = new DbSession(null!, new SqlConnection());
    var query = s.Users.Builder().Delete();
    await Assert.That(query.Sql).IsEqualTo("delete from dbo.[TUsers]");
  }

  [Test]
  public async Task Build_Delete_Where()
  {
    var s = new DbSession(null!, new SqlConnection());
    var query = s.Users.Delete($"Id = {"u1"}");
    await Assert.That(query.Sql).IsEqualTo("delete from dbo.[TUsers] where Id = @p0");
  }

  [Test]
  public async Task Build_Then_InsertThenDelete()
  {
    var s = new DbSession(null!, new SqlConnection());
    var query = s.Users.Insert("Id, Name", $"{1},{"a"}").Then().Delete($"Id = {1}");
    await Assert.That(query.Sql).IsEqualTo("insert into dbo.[TUsers] (Id, Name) values (@p0,@p1);\n\ndelete from dbo.[TUsers] where Id = @p2");
  }

  [Test]
  public async Task Build_Raw()
  {
    var s = new DbSession(null!, new SqlConnection());
    var query = s.Users.Raw($"select 1 as One");
    await Assert.That(query.Sql).IsEqualTo("select 1 as One");
  }

  [Test]
  public async Task Build_SelectChained()
  {
    var s = new DbSession(null!, new SqlConnection());
    var query = s.Users.Select($"Id").Select($"Name");
    await Assert.That(query.Sql).IsEqualTo("select Id, Name from dbo.[TUsers]");
  }

  [Test]
  public async Task Build_WhereChained()
  {
    var s = new DbSession(null!, new SqlConnection());
    var query = s.Users.Where($"Id = {"x"}").Where($"Name = {"y"}");
    await Assert.That(query.Sql).IsEqualTo("select * from dbo.[TUsers] where Id = @p0 and Name = @p1");
  }

  [Test]
  public async Task Build_In_Collection()
  {
    var s = new DbSession(null!, new SqlConnection());
    IEnumerable<string> ids = new[] { "a", "b", "c" };
    var query = s.Users.Where($"{Dbo.CN("Id")} in ({ids})");
    await Assert.That(query.Sql).IsEqualTo("select * from dbo.[TUsers] where [Id] in ((select value from string_split(@p0, char(1))))");
  }

  [Test]
  public async Task Build_Union()
  {
    var s = new DbSession(null!, new SqlConnection());
    var query = s.Users.Union([
      b => b.Select($"Id"),
      b => b.Select($"Name")
    ]);
    await Assert.That(query.Sql).IsEqualTo("select t.* from (select Id from dbo.[TUsers] union select Name from dbo.[TUsers]) t");
  }

  [Test]
  public async Task Build_UnionAll()
  {
    var s = new DbSession(null!, new SqlConnection());
    var query = s.Users.UnionAll([
      b => b.Select($"Id"),
      b => b.Select($"Name")
    ]);
    await Assert.That(query.Sql).IsEqualTo("select t.* from (select Id from dbo.[TUsers] union all select Name from dbo.[TUsers]) t");
  }

  [Test]
  public async Task Build_Table_AlternativeSource()
  {
    var s = new DbSession(null!, new SqlConnection());
    var query = s.Users.Table("UsersView").Where("Id = @p0");
    await Assert.That(query.Sql).IsEqualTo("select * from dbo.[UsersView] where Id = @p0");
  }
}

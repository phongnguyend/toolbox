using System;
using System.IO;
using System.Linq;

namespace AzureAccountGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadAllLines("input.txt");
            var accounts = lines.Select(x =>
            {
                var parts = x.Split(",");
                return new
                {
                    Name = parts[0].Trim(),
                    DisplayName = parts[0].Trim(),
                    Domain = parts[1].Trim(),
                    Subscription = parts[2].Trim(),
                    GroupName = parts[0].Trim(),
                    Password = parts[3].Trim(),
                    StorageAccountName = "sa" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 22),
                    SqlServerName = "sqlserver" + Guid.NewGuid().ToString().Replace("-", ""),
                    WebApp1Name = "backend" + Guid.NewGuid().ToString().Replace("-", ""),
                    WebApp2Name = "customersite" + Guid.NewGuid().ToString().Replace("-", ""),
                };
            });


            var createTemplate = File.ReadAllText("create.txt");
            var deleteTemplate = File.ReadAllText("delete.txt");

            var domains = accounts.GroupBy(x => x.Domain);

            foreach (var domain in domains)
            {
                using var writer1 = new StreamWriter("../../../" + domain.Key + ".create.txt");
                using var writer2 = new StreamWriter("../../../" + domain.Key + ".delete.txt");
                foreach (var account in domain)
                {
                    var text = createTemplate;
                    text = text.Replace("<Display Name>", account.DisplayName);
                    text = text.Replace("<Password>", account.Password);
                    text = text.Replace("<Name>", account.Name);
                    text = text.Replace("<Storage Account Name>", account.StorageAccountName.ToLower());
                    text = text.Replace("<Sql Server Name>", account.SqlServerName.ToLower());
                    text = text.Replace("<WebApp1 Name>", account.WebApp1Name.ToLower());
                    text = text.Replace("<WebApp2 Name>", account.WebApp2Name.ToLower());
                    text = text.Replace("<Domain>", account.Domain);
                    text = text.Replace("<Group Name>", account.GroupName);
                    text = text.Replace("<Subscription>", account.Subscription);

                    writer1.WriteLine(text);

                    text = deleteTemplate;
                    text = text.Replace("<Display Name>", account.DisplayName);
                    text = text.Replace("<Password>", account.Password);
                    text = text.Replace("<Name>", account.Name);
                    text = text.Replace("<name>", account.Name.ToLower());
                    text = text.Replace("<Domain>", account.Domain);
                    text = text.Replace("<Group Name>", account.GroupName);
                    text = text.Replace("<Subscription>", account.Subscription);

                    writer2.WriteLine(text);
                }
            }

        }


    }
}

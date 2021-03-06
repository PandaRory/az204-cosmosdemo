﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace az204_cosmosdemo
{
    class Program
    {
        // The Azure Cosmos DB endpoint for running this sample.
        private static readonly string EndpointUri = "https://20201212-cosmos-db.documents.azure.com:443/";
        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = "wa9DfZQZ3r1nM34WDDUKVpQ9eaxmwCw3XMYacY7WS7nQhYMAL7Nhn9ZuPkDCXAlLZTDqEkqhZ5LmlAqIHIxSDA==";

        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database;

        // The container we will create.
        private Container container;

        // The name of the database and container we will create
        private string databaseId = "az204Database";
        private string containerId = "az204Container";
    
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Beginning operations...\n");
                Program p = new Program();
                await p.CosmosDemoAsync();

            }
            catch (CosmosException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        public async Task CosmosDemoAsync()
        {
            // Create a new instance of the Cosmos Client
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            // Runs the CreateDatabaseAsync method
            await this.CreateDatabaseAsync();
            // Run the CreateContainerAsync method
            await this.CreateContainerAsync();

            // 新增資料
            await this.InsertData();
            await this.QueryData();
        }

        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            this.database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }
        private async Task CreateContainerAsync()
        {
            // Create a new container
            this.container = await this.database.CreateContainerIfNotExistsAsync(containerId, "/LastName");
            Console.WriteLine("Created Container: {0}\n", this.container.Id);
        }
        private dynamic getFakeData()
        {
            string url = "https://api.namefake.com/";
            HttpClient client = new HttpClient();
            HttpResponseMessage response = client.GetAsync(url).Result;
            string responseBody = response.Content.ReadAsStringAsync().Result;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseBody);
        }

        private async Task InsertData()
        {
            for (int i = 0; i < 10; i++)
            {
                var person = getFakeData();
                var name = person.name.ToString().Split(" ");
                var lastName = name[name.Length - 1];
                var firstName = name[name.Length - 2];
                var rec = new DataRecord()
                {
                    id = Guid.NewGuid().ToString(),
                    FirstName = firstName,
                    LastName = lastName,
                    Address = person.address,
                    FullName = person.name.ToString()
                };
                var item = await this.container.CreateItemAsync<DataRecord>(rec, new PartitionKey(lastName));
                Console.WriteLine("Created item {0}: \n{1}\n", i, Newtonsoft.Json.JsonConvert.SerializeObject(item));
            }
            Console.WriteLine("\n100 items has been added...");
        }

        private async Task QueryData()
        {
            //查詢
            var sqlQueryText = "SELECT * FROM c WHERE CONTAINS(c.FullName,'ab') ";
            Console.WriteLine("執行查詢: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<DataRecord> queryResultSetIterator =
            this.container.GetItemQueryIterator<DataRecord>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                Console.WriteLine("\ntest");
                FeedResponse<DataRecord> currentResultSet = queryResultSetIterator.ReadNextAsync().Result;
                foreach (DataRecord DataRecord in currentResultSet)
                {
                    Console.WriteLine("\nitem {0}", DataRecord.FullName);
                }
            }
        }
    }

    class DataRecord
    {
        public string id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Address { get; set; }
        public string FullName { get; set; }
    }
}

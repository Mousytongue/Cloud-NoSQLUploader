using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.IO;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;

namespace Prog4WebDatabase
{
    public partial class _Default : Page
    {
        //Site to pull data from
        string uri = "https://css490.blob.core.windows.net/lab4/input.txt";
        string blobName = "blob4mousytongue";
        string tableName = "table4mousytongue";
        string fileName = "UploadedData.txt";


        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void LoadDataBtn_Click(object sender, EventArgs e)
        {
            Results1.Text = "";
            ResultTitle.Text = "";
            Error1.Text = "";
            LoadFromSite();
        }

        protected void ClearDataBtn_Click(object sender, EventArgs e)
        {
            Results1.Text = "";
            ResultTitle.Text = "";
            Error1.Text = "";
            RemoveFromBlob();
            RemoveFromTable();
        }

        protected void QueryBtn_Click(object sender, EventArgs e)
        {
            Results1.Text = "";
            ResultTitle.Text = "";
            Error1.Text = "";
            ProcessQuery();
        }

        void ProcessQuery()
        {
            ResultTitle.Text = "Query Results";
            string firstName = TextBox_firstname.Text;
            string lastName = TextBox_lastname.Text;

            //Setup connection to the azure table
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["TableConnectionString"].ConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable entryTable = tableClient.GetTableReference(tableName);

            //Only partition key
            if (firstName == string.Empty && lastName != string.Empty)
            {
                TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, lastName));
                TableContinuationToken token = null;

                do
                {
                    TableQuerySegment<DynamicTableEntity> resultSegment = entryTable.ExecuteQuerySegmented(query, token);
                    token = resultSegment.ContinuationToken;
                    foreach (DynamicTableEntity entity in resultSegment.Results)
                    {
                        IDictionary<string, EntityProperty> properties = entity.Properties;

                        Results1.Text += entity.PartitionKey + ", " + entity.RowKey + ": ";
                        foreach (KeyValuePair<string, EntityProperty> entry in properties)
                        {
                            Results1.Text += entry.Key + " = " + entry.Value.StringValue + "; ";
                        }
                        Results1.Text += "<br />";
                    }


                } while (token != null);
            }

            //Only row key
            else if (firstName != string.Empty && lastName == string.Empty)
            {
                TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, firstName));
                TableContinuationToken token = null;

                do
                {
                    TableQuerySegment<DynamicTableEntity> resultSegment = entryTable.ExecuteQuerySegmented(query, token);
                    token = resultSegment.ContinuationToken;
                    foreach (DynamicTableEntity entity in resultSegment.Results)
                    {
                        IDictionary<string, EntityProperty> properties = entity.Properties;

                        Results1.Text += entity.PartitionKey + ", " + entity.RowKey + ": ";
                        foreach (KeyValuePair<string, EntityProperty> entry in properties)
                        {
                            Results1.Text += entry.Key + " = " + entry.Value.StringValue + "; ";
                        }
                        Results1.Text += "<br />";
                    }


                } while (token != null);
            }

            //Using both keys
            else if (firstName != string.Empty && lastName != string.Empty)
            {
                TableOperation retrieveOp = TableOperation.Retrieve<DynamicTableEntity>(lastName, firstName);
                TableResult retrieved = entryTable.Execute(retrieveOp);

                if (retrieved.Result != null)
                {
                    IDictionary<string, EntityProperty> properties = ((DynamicTableEntity)retrieved.Result).Properties;

                    Results1.Text = ((DynamicTableEntity)retrieved.Result).PartitionKey + ", " + ((DynamicTableEntity)retrieved.Result).RowKey + ": ";
                    foreach (KeyValuePair<string, EntityProperty> entry in properties)
                    {
                        Results1.Text += entry.Key + " = " + entry.Value.StringValue + "; ";
                    }
                }
            }
            if (Results1.Text == string.Empty)
                Results1.Text = "No results found. <br />Needs exact spelling.";

        }

        void LoadFromSite()
        {
            List<string> entries = new List<string>();

            //creates a streamreader from the txt off the designated website
            StreamReader reader = PullFromSite();
            //Fill the list of entries with each line of the text
            while (reader.Peek() >= 0)
            {
                string entry = reader.ReadLine();
                entries.Add(entry);
            }

            //Displays the entries to user
            //DisplayUploadResults(entries);

            //Loads the text to a blob storage
            UploadToBlob(entries);

            //Loads to azure tables
            UploadToTable(entries);
        }

        void UploadToTable(List<string> entries)
        {
            try
            {
                Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["TableConnectionString"].ConnectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable entryTable = tableClient.GetTableReference(tableName);
                entryTable.CreateIfNotExistsAsync().Wait();

                ResultTitle.Text = "";
                Results1.Text = "";
                Error1.Text = "";
                foreach (string entry in entries)
                {
                    //Parse the string..  
                    //Attempts to remove tabs and white spaces, doesnt fix the Robert williams case.
                    char tab = '\u0009';
                    string newEntry = entry.Replace(tab.ToString(), " ");
                    newEntry.Trim();
                    string[] arr = newEntry.Split();

                    //Jerry rigged to fix the robert williams case where the first substring is empty
                    int counter = 0;
                    if (arr[0] == string.Empty)
                    {
                        //Error1.Text += "String[0] is empty";
                        counter++;
                    }

                    string firstName = arr[counter];
                    string lastName = arr[counter + 1];

                    var entity = new DynamicTableEntity(lastName, firstName);

                    string valuesToPrint = "";
                    if (arr.Length > 2)
                    {
                        for (int i = 2; i < arr.Length; i++)
                        {
                            int index = arr[i].IndexOf('=');
                            if (index == -1)
                                continue;
                            string key = arr[i].Substring(0, index);
                            string value = arr[i].Substring(index + 1);

                            entity.Properties.Add(key, new EntityProperty(value));

                            valuesToPrint += key + "=" + value + " ";
                        }
                    }

                    var mergeOperation = TableOperation.InsertOrMerge(entity);
                    entryTable.ExecuteAsync(mergeOperation);

                    ResultTitle.Text = "Merged into table:";
                    Results1.Text += "Name: " + lastName + ", " + firstName + "    " + valuesToPrint + "<br />";
                }
            }
            catch (Exception ex)
            {
                Error1.Text = "Failed to upload to table. <br />If you just cleared the table, wait for it to finish deletion.";
            }
        }

        void RemoveFromTable()
        {
            try
            {
                //Setup connection to the azure table
                Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["TableConnectionString"].ConnectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable entryTable = tableClient.GetTableReference(tableName);

                //Query all rows then calls them to be deleted
                var entities = entryTable.ExecuteQuery(new TableQuery<DynamicTableEntity>()).ToList();
                foreach (var row in entities)
                {
                    TableOperation deleteOP = TableOperation.Delete(row);
                    entryTable.ExecuteAsync(deleteOP);
                }

                //Print update status to user
                Results1.Text += "<br />Removed from Azure Table";
            }
            catch (Exception ex)
            {
                Error1.Text = "Failed to remove from table";
            }
        }

        StreamReader PullFromSite()
        {
            WebResponse response = null;
            StreamReader reader = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "GET";
                response = request.GetResponse();
                reader = new StreamReader(response.GetResponseStream());
            }
            //If unable to connect, returns a custom string so I can report a custom message, instead of crashing program.
            catch (Exception ex)
            {
                Error1.Text = "Error Pulling data from webpage" + ex;
            }
            return reader;
        }

        void DisplayUploadResults(List<string> entries)
        {
            string result = "";

            foreach (string entry in entries)
            {
                result += entry + "<br />";
            }
            ResultTitle.Text = "Uploaded:";
            Results1.Text = result;
        }

        void UploadToBlob(List<string> entries)
        {
            string data = "";
            foreach (string item in entries)
            {
                data += item + "\n";
            }

            try
            {
                //Setup connection to the azure blob storage
                Microsoft.Azure.Storage.CloudStorageAccount storageAccount = Microsoft.Azure.Storage.CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["TableConnectionString"].ConnectionString);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(blobName);
                container.CreateIfNotExistsAsync().Wait();
                CloudBlockBlob blob = container.GetBlockBlobReference(fileName);

                //Upload the data string to storage
                blob.UploadTextAsync(data);
            }
            catch (Exception ex)
            {
                Error1.Text = "Failed to upload to blob storage. ";
            }
        }

        void RemoveFromBlob()
        {
            try
            {
                //Setup connection to the azure blob storage
                Microsoft.Azure.Storage.CloudStorageAccount storageAccount = Microsoft.Azure.Storage.CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["TableConnectionString"].ConnectionString);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(blobName);
                CloudBlockBlob blob = container.GetBlockBlobReference(fileName);

                //Deletes the whole blob
                blob.DeleteAsync();

                //Update status to user
                ResultTitle.Text = "Status:";
                Results1.Text = "";
                Results1.Text = "Removed from Blob Storage";
            }
            catch (Exception ex)
            {
                Error1.Text = "Failed to remove from blob storage. ";
            }
        }
    }
}
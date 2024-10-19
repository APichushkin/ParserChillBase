using System;
using System.Globalization;
using System.IO;
using CsvHelper;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ParserChillBase
{
    internal class Program
    {
        private static string sheetId = "1t1bveuMPVhGsz4tKbhGbcIGhQjk9UbInuxsQiH1wxyM";
        private static string gid = "0";

        static async Task Main(string[] args)
        {
            await GetSCVData();
        }

        private static async Task GetSCVData()
        {
            string url = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={gid}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string csvData = await response.Content.ReadAsStringAsync();

                    using (TextReader reader = new StringReader(csvData))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<LineElement>();
                        SetupData(records);
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("Ошибка при запросе данных: " + e.Message);
                }
            }
        }

        private static void SetupData(IEnumerable<LineElement> scvRecords)
        {
            var finalData = new List<BalanceElement>();
            foreach (var record in scvRecords)
            {
                
                CheckRegionElement(finalData, record.Date, record.CIS, "CIS");
                CheckRegionElement(finalData, record.Date, record.EU, "EU");
                CheckRegionElement(finalData, record.Date, record.UK, "UK");
            }

            List<BalanceJSONStructure> CISBalanceList = new List<BalanceJSONStructure>();
            List<BalanceJSONStructure> EUBalanceList = new List<BalanceJSONStructure>();
            List<BalanceJSONStructure> UKBalanceList = new List<BalanceJSONStructure>();

            

            foreach (var element in finalData)
            {
                switch (element.Region)
                {
                    case "CIS":
                        CISBalanceList.Add(new BalanceJSONStructure(element.DateStart, element.DateEnd, element.Balance));
                        break;
                    case "EU":
                        EUBalanceList.Add(new BalanceJSONStructure(element.DateStart, element.DateEnd, element.Balance));
                        break;
                    case "UK":
                        UKBalanceList.Add(new BalanceJSONStructure(element.DateStart, element.DateEnd, element.Balance));
                        break;
                    default:
                        break;
                }
            }

            RootObject rootObject = new RootObject
            {
                CIS_Balance = CISBalanceList,
                EU_Balance = EUBalanceList,
                UK_Balance = UKBalanceList,
            };
            GenerateJSON(rootObject);
        }

        private static void GenerateJSON(RootObject finalData)
        {
            string json = JsonConvert.SerializeObject(finalData, Newtonsoft.Json.Formatting.Indented);

            string filePath = "D:/ConfigTask3.json";

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine(json);
                }
                Console.WriteLine($"JSON успешно записан в файл: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка при записи в файл: {ex.Message}");
            }
        }

        private static void CheckRegionElement(List<BalanceElement> data, string date, string balance, string region)
        {
            if (balance != "")
            {
                if (balance.Contains(","))
                {
                    var recordSubstrings = balance.Split(',');
                    foreach (var recordSubstring in recordSubstrings)
                    {
                        ChangeBalanceElement(data, date, recordSubstring, region);
                    }
                }
                else
                {
                    ChangeBalanceElement(data, date, balance, region);
                }
            }
        }

        private static void ChangeBalanceElement(List<BalanceElement> data, string date, string balance, string region)
        {
            if (data.Exists(x => x.Region == region && x.Balance == balance))
            {
                data.Find(x => x.Region == region && x.Balance == balance).DateEnd = date;
            }
            else
            {
                data.Add(new BalanceElement(date, date, balance, region));
            }
        }
    }


    public class LineElement
    {
        public string Date { get; set; }
        public string CIS { get; set; }
        public string EU { get; set; }
        public string UK { get; set; }
    }

    public class BalanceElement
    {
        public string DateStart { get; set; }
        public string DateEnd { get; set; }
        public string Balance { get; set; }
        public string Region { get; set; }

        public BalanceElement(string dateStart, string dateEnd, string balance, string region)
        {
            DateStart = dateStart;
            DateEnd = dateEnd;
            Balance = balance;
            Region = region;
        }
    }

    public class BalanceJSONStructure
    {
        public string start_date { get; set; }
        public string end_date { get; set; }
        public string balance { get; set; }

        public BalanceJSONStructure(string start_date, string end_date, string balance)
        {
            this.start_date = start_date;
            this.end_date = end_date;
            this.balance = balance;
        }
    }

    public class RootObject
    {
        public List<BalanceJSONStructure> CIS_Balance { get; set; }
        public List<BalanceJSONStructure> EU_Balance { get; set; }
        public List<BalanceJSONStructure> UK_Balance { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using HtmlAgilityPack;
using System.Net.Http;

namespace BankAccount
{
    public enum currency { UAH = 980, USD = 840, EUR = 978, PLN = 985 }

    delegate void AccountHandler(string message);

    public interface ICredit
    {
        void Credit(double sum, currency c);
    }

    public class Account
    {
        private int ID;
        private string Name;
        private double Balance;
        private currency C;
        static int AccNum = 0;


        public int id
        {
            get { return ID; }
            set { ID = value; }
        }
        public string name
        {
            get { return Name; }
            set { Name = value; }
        }
        public double balance
        {
            get { return Balance; }
            set { Balance = value; }
        }

        public currency c
        {
            get { return C; }
            set { C = value; }
        }

        public Account() : this(0, "", 0, currency.UAH)
        {

        }

        public Account(int id, string name, double balance, currency c)
        {
            ID = id;
            Name = name;
            Balance = balance;
            C = c;
            AccNum++;
        }
    }

    public class Currency
    {
        private int IDnC;
        private string IDcC;
        private int QuantilyC;
        private string NameC;
        private double AmountC;


        public int idnc
        {
            get { return IDnC; }
            set { IDnC = value; }
        }
        public string idcc
        {
            get { return IDcC; }
            set { IDcC = value; }
        }
        public int quantilyc
        {
            get { return QuantilyC; }
            set { QuantilyC = value; }
        }
        public string namec
        {
            get { return NameC; }
            set { NameC = value; }
        }
        public double amountc
        {
            get { return AmountC; }
            set { AmountC = value; }
        }
        public Currency() : this(0, "", 0, "", 0)
        {
            
        }

        public Currency(int idnc, string idcc, int quantilyc, string namec, double amountc)
        {
            IDnC = idnc;
            IDcC = idcc;
            QuantilyC = quantilyc;
            NameC = namec;
            AmountC = amountc;
        }
    }

    public class CurrentCounter : Account, ICredit
    {
        event AccountHandler Notify;

        public CurrentCounter() : base(0, "", 0, currency.UAH)
        {
        }

        public CurrentCounter(int ID, string Name, double Balance, currency C) : base(ID, Name, Balance, C)
        {
            id = ID;
            name = Name;
            balance = Balance;
            c = C;
        }

        public void Credit(double amount, currency e)
        {
            try
            {

                if (balance >= Exchange(amount, c, e))
                {
                    balance -= Exchange(amount, c, e);
                    Notify?.Invoke($"Списання з рахунку: {amount} {e}");
                    History registor = new History($"Списання з рахунку: {amount} {e}");
                }
                else
                {
                    Notify?.Invoke("Недостатньо коштів");
                    History registor = new History("Недостатньо коштiв");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Debit(double amount, currency e)
        {
            try
            {
                balance += Exchange(amount, c, e);
                Notify?.Invoke($"Поповнення рахунку: {amount} {e}");
                History registor = new History($"Поповнення рахунку: {amount} {e}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        public double Exchange(double amount, currency с, currency e)
        {
            File.Delete(@"Currencylist.txt");
            string url = "https://bank.gov.ua/ua/markets/exchangerates?date=01.10.2021&period=daily";

            try
            {
                using (HttpClientHandler hd1 = new HttpClientHandler { AllowAutoRedirect = false, AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.None })
                {
                    using (var clnt = new HttpClient(hd1))
                    {
                        using (HttpResponseMessage resp = clnt.GetAsync(url).Result)
                        {
                            if (resp.IsSuccessStatusCode)
                            {
                                var html = resp.Content.ReadAsStringAsync().Result;

                                if (!string.IsNullOrEmpty(html))
                                {
                                    HtmlDocument doc = new HtmlDocument();
                                    doc.LoadHtml(html);

                                    var list = doc.DocumentNode.SelectNodes(".//div[@class='container fit']//div[@class='row']//div[@class='col-md-12 wc widget-tableWithSearch']//div[@class='row']//div[@class='col-md-8']//div[@class='widget']//div[@class='widget-content']//div[@class='outer']//div[@class='inner']//table[@id='exchangeRates']//tbody//tr");
                                    if (list != null && list.Count > 0)
                                    {

                                        foreach (var b_currency in list)
                                        {
                                            string all = b_currency.SelectSingleNode(".").InnerText;

                                            var arr = all.Split(new char[] { ' ', '\n', '\r', '"', }, StringSplitOptions.RemoveEmptyEntries);

                                            foreach(string k in arr)
                                            {
                                                File.AppendAllText(@"Currencylist.txt", k + " ");
                                            }
                                            File.AppendAllText(@"Currencylist.txt", "\n");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("No information");
                                    }

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            List<Currency> List_C = File.ReadAllLines("Currencylist.txt").Select(y => y.GetCurrency()).ToList();

            int i = 0;
            double amount_USD = 1, amount_EUR = 1, amount_PLN = 1;

            foreach (Currency currency in List_C)
            {
                if (currency.idnc == 840)
                {
                    amount_USD = currency.amountc;
                }
                if (currency.idnc == 978)
                {
                    amount_EUR = currency.amountc;
                }
                if (currency.idnc == 985)
                {
                    amount_PLN = currency.amountc;
                }
                i++;
            }


            switch (e)
            {
                case currency.USD:
                    {
                        switch (c)
                        {
                            case currency.UAH:
                                {
                                    amount *= amount_USD;
                                    break;
                                }

                            case currency.EUR:
                                {
                                    amount *= amount_USD/amount_EUR;
                                    break;
                                }

                            case currency.PLN:
                                {
                                    amount *= amount_USD/amount_PLN;
                                    break;
                                }
                        }
                        break;
                    }

                case currency.UAH:
                    {
                        switch (c)
                        {
                            case currency.USD:
                                {
                                    amount *= 1/amount_USD;
                                    break;
                                }

                            case currency.EUR:
                                {
                                    amount *= 1/amount_EUR;
                                    break;
                                }

                            case currency.PLN:
                                {
                                    amount *= 1/amount_PLN;
                                    break;
                                }
                        }
                        break;
                    }

                case currency.EUR:
                    {
                        switch (c)
                        {
                            case currency.USD:
                                {
                                    amount *= amount_EUR/amount_USD;
                                    break;
                                }

                            case currency.UAH:
                                {
                                    amount *= amount_EUR;
                                    break;
                                }

                            case currency.PLN:
                                {
                                    amount *= amount_EUR/amount_PLN;
                                    break;
                                }
                        }
                        break;
                    }

                case currency.PLN:
                    {
                        switch (e)
                        {
                            case currency.USD:
                                {
                                    amount *= amount_PLN/amount_USD;
                                    break;
                                }

                            case currency.UAH:
                                {
                                    amount *= amount_PLN;
                                    break;
                                }

                            case currency.EUR:
                                {
                                    amount *= amount_PLN/amount_EUR;
                                    break;
                                }
                        }
                        break;
                    }
            }
            return amount;
        }

        public void Show()
        {
            Console.WriteLine(balance);
        }
    }

    public class History
    {
        public History(string register)
        {
            File.AppendAllText(@"History.txt", register + "\n");
        }

        public static void Show()
        {
            List<string> history = File.ReadLines("History.txt").ToList();
            foreach (string i in history)
                Console.WriteLine(i);
        }
    }

    public static class StringExt
    {
        public static Account ToAccount(this string value)
        {
            var arr = value.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            return new Account()
            {
                id = Convert.ToInt32(arr[0]),
                name = arr[1],
                balance = Convert.ToDouble(arr[2]),
                c = (currency)Enum.Parse(typeof(currency), arr[3]),
            };
        }

        public static Currency GetCurrency(this string list)
        {
            var arr = list.Split(new char[] { ' ', '\n', '\r', '"', }, StringSplitOptions.RemoveEmptyEntries);
            

            double test = 0;
            int i = 4;

            while (test == 0)
            {
                try
                {
                    test = Convert.ToDouble(arr[i]);
                }
                catch (Exception) 
                { 
                    arr[3] = arr[3] + " " + arr[i];
                    i++;
                }
            }

            return new Currency()
            {
                idnc = Convert.ToInt32(arr[0]),
                idcc = arr[1],
                quantilyc = Convert.ToInt32(arr[2]),
                namec = arr[3],
                amountc = Convert.ToDouble(arr[i]),
            };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            File.Delete(@"History.txt");
            

            List<Account> client = File.ReadAllLines("test.txt").Select(x => x.ToAccount()).ToList();

            CurrentCounter acc_0 = new CurrentCounter(client[0].id, client[0].name, client[0].balance, client[0].c);
            acc_0.Debit(200, currency.EUR);

            CurrentCounter acc_1 = new CurrentCounter(client[1].id, client[1].name, client[1].balance, client[1].c);
            acc_1.Credit(740, currency.UAH);
            //
            CurrentCounter acc_2 = new CurrentCounter(client[2].id, client[2].name, client[2].balance, client[2].c);
            acc_2.Credit(423, currency.EUR);
            //
            CurrentCounter acc_3 = new CurrentCounter(client[3].id, client[3].name, client[3].balance, client[3].c);
            acc_3.Debit(500, currency.USD);
            //
            CurrentCounter acc_4 = new CurrentCounter(client[4].id, client[4].name, client[4].balance, client[4].c);
            acc_4.Debit(245, currency.EUR);
            //
            CurrentCounter acc_5 = new CurrentCounter(client[5].id, client[5].name, client[5].balance, client[5].c);
            acc_5.Debit(2852, currency.UAH);
            //
            CurrentCounter acc_6 = new CurrentCounter(client[6].id, client[6].name, client[6].balance, client[6].c);
            acc_6.Credit(500, currency.PLN);

            CurrentCounter acc_7 = new CurrentCounter(client[7].id, client[7].name, client[7].balance, client[7].c);
            acc_7.Debit(400, currency.EUR);

            CurrentCounter acc_8 = new CurrentCounter(client[8].id, client[8].name, client[8].balance, client[8].c);
            acc_8.Credit(250, currency.USD);
            //
            CurrentCounter acc_9 = new CurrentCounter(client[9].id, client[9].name, client[9].balance, client[9].c);
            acc_9.Credit(2000, currency.UAH);


            History.Show();
        }
    }
}
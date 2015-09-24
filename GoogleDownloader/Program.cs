using System;
using System.Net;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace GoogleDownloader
{
	class MainClass
	{
		public static void CreateZip(string fullpath,string zipfilename,string file)
		{
			//string zipfile = symbol + ".zip";
			string curdir = Directory.GetCurrentDirectory();
			string zipfullpath = Path.Combine(curdir,zipfilename);
			File.Delete(zipfullpath);
			using(ZipArchive _zip = ZipFile.Open(zipfullpath,ZipArchiveMode.Create))
			{
				_zip.CreateEntryFromFile(fullpath,file);
			}
		}
		public static DateTime FromUnixTime(long unixTime)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddSeconds(unixTime);
		}
		public static void Main (string[] args)
		{
			//q=SYMBOL
			//i=resolution in seconds
			//p=period in days
			var urlPrototype = @"http://www.google.com/finance/getprices?q={0}&i={1}&p={2}d&f=d,c,h,l,o,v";
			if (args.Length != 3) {
				Console.WriteLine ("Usage: GoogleDownloader SYMBOL RESOLUTION PERIOD");
				Console.WriteLine ("SYMBOL = eg SPY");
				Console.WriteLine ("RESOLUTION = 60 for minute intraday data");
				Console.WriteLine ("PERIOD = 10 for 10 days intraday data");
				Environment.Exit (1);
			}
			var symbol = args [0];
			var resolution = args [1];
			var period = args [2];
			// The Yahoo Finance URL for each parameter
			var url = string.Format(urlPrototype, symbol, resolution,period);
			try
			{
				WebClient cl = new WebClient();
				var lines = cl.DownloadString(url);
				var lines_ = lines.Split('\n');
				string temppath = Path.GetTempPath();
				//var firstline = lines_[7].Split(',');
				var i = 7;
				while (true)
				{
					if (i >= lines_.Length-1)
						break;
					bool file_open = false;
					string fullpath = "";
					var zipfilename = "";
					var filename = "";
					if (lines_[i][0] == 'a')
					{
						long unixtime;
						var str_ = lines_[i].Split(',');
						var st = str_[0].Remove(0,1);
						Int64.TryParse(st, out unixtime);
						var dt = FromUnixTime(unixtime);
						var dt_str = dt.ToString("yyyyMMdd");
						filename = string.Format("{0}_minute_trade.csv",dt_str,symbol.ToLower());
						zipfilename = string.Format("{0}_trade.zip",dt_str);
						fullpath = Path.Combine(temppath,filename);
						File.Delete(fullpath);
						file_open = true;
					}
					if (file_open)
					{
						//we assume time start is 3:30 PM
						//even though Google says 1:30 PM
						var start_time = 5.58e+7;
						using(StreamWriter swfile = new System.IO.StreamWriter(fullpath))
						{
							int first_seen = 0;
							while(true)
							{	
								if (i >= lines_.Length-1)
									break;
								var str_ = lines_[i].Split(',');
								if (str_.Length < 6)
									break;
								if (str_[0][0]=='a')
									first_seen += 1;
								if (first_seen > 1)
									break;
								
								var open = 0.0m;
								var high = 0.0m;
								var low = 0.0m;
								var close = 0.0m;
								int volume = 0;
								var min = 0;

								Int32.TryParse(str_[0],out min);
								var time = start_time + 60000*min;

								Decimal.TryParse(str_[1],out open);
								Decimal.TryParse(str_[2],out high);
								Decimal.TryParse(str_[3],out low);
								Decimal.TryParse(str_[4],out close);
								Int32.TryParse(str_[5],out volume);
								int _open, _high, _low, _close;
								_open = Decimal.ToInt32(10000*open);
								_high = Decimal.ToInt32(10000*high);
								_low = Decimal.ToInt32(10000*low);
								_close = Decimal.ToInt32(10000*close);
								var s = "{0},{1},{2},{3},{4},{5}";
								var sf = string.Format(s,time,_open,_high,_low,_close,volume);
								swfile.WriteLine(sf);
								i+=1;
							}
						

						}
						CreateZip(fullpath,zipfilename,filename);
						file_open = false;
					}
				}

			}
			catch(Exception ex)
			{
				Console.Write("Error: " + ex.Message);
				Environment.Exit(1);
			}

		}

	}

}

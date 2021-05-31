using System;
using System.Collections.Generic;
using System.Net;
using tik4net;

namespace MikrotikUtils
{
	public class MicrotikClient
	{
		private string Ip { get; }
		private string Password { get; }
		public ITikConnection Connection { get; set; }


		public MicrotikClient(string ip, string password)
		{
			Ip = ip;
			Password = password;
		}

		public void CreateConnection()
		{
			Connection = ConnectionFactory.OpenConnection(TikConnectionType.Api, Ip, "port", Password);
		}

		public void RemoveIpAddressFromList()
		{
			string[] commands = new string[] {
				"/ip/firewall/address-list/print",
				"?list=BlackList",
				//"=.proplist=address"
            };
			IEnumerable<ITikSentence> result = Connection.CallCommandSync(commands);
			Print(result);
		}

		public void OpenPort()
		{
			Console.WriteLine("Введите IP");
			string ipAddress = Console.ReadLine();


			Console.WriteLine("Введите IP или введите 0 для автоопределения IP");
			string ip = Console.ReadLine();
			if (ip == "0")
			{
				ip = GetIp().ToString();
			}

			Console.WriteLine("Введите внутренний порт");
			string internalPort = Console.ReadLine();

			Console.WriteLine("Введите внешний порт");
			string externalPort = Console.ReadLine();

			string[] commands = new string[] {
				"/ip/firewall/nat/add",
				"=chain=dstnat",
				"=action=dst-nat",
				"=in-interface=ether1-gateway",
				"=protocol=tcp",
				string.Format("=dst-port={0}", externalPort),
				string.Format("=to-addresses={0}", ip),
				string.Format("=to-ports={0}", internalPort)
			};

			IEnumerable<ITikSentence> result = Connection.CallCommandSync(commands);

			Print(result);
		}

		private void Print(IEnumerable<ITikSentence> result)
		{
			foreach (ITikSentence sentence in result)
			{
				if (sentence is ITikTrapSentence)
					Console.WriteLine("Сообщение об ошибке: {0}", ((ITikTrapSentence)sentence).Message);
				else if (sentence is ITikDoneSentence)
					Console.WriteLine("Команда выполнена");
				else if (sentence is ITikReSentence)
				{
					foreach (var wordPair in sentence.Words)
					{
						Console.WriteLine("  {0}={1}", wordPair.Key, wordPair.Value);
					}
				}
				else
					throw new NotImplementedException("Неизвестная команда");
			}
		}

		private IPAddress GetIp()
		{
			String host = System.Net.Dns.GetHostName();
			// Получение ip-адреса.
			IPAddress ip = System.Net.Dns.GetHostByName(host).AddressList[0];
			return ip;

		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Введите пароль");
			string password = Console.ReadLine();

			var client = new MicrotikClient("192.168.1.1", password);
			client.CreateConnection();

			Dictionary<string, Delegate> operations = new Dictionary<string, Delegate>();


			operations.Add("remove", new Action(client.RemoveIpAddressFromList));
			operations.Add("open", new Action(client.OpenPort));

			ConsoleKeyInfo input;
			do
			{
				Console.WriteLine("Название операции");
				string operationName = Console.ReadLine();

				operations[operationName].DynamicInvoke();

				input = Console.ReadKey();
			} while (input.Key != ConsoleKey.Escape);
		}
	}
}

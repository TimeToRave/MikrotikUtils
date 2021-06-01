using System;
using System.Collections.Generic;
using System.Net;
using tik4net;

namespace MikrotikUtils
{
	public class MicrotikClient
	{
		private ITikConnection connection;
		private string Ip { get; }
		private string Login { get; }
		private string Password { get; }
		public ITikConnection Connection
		{
			get
			{
				if (connection == null)
				{
					connection = ConnectionFactory.OpenConnection(TikConnectionType.Api, Ip, Login, Password);
				}
				return connection;
			}
		}

		/// <summary>
		/// Основной конструктор
		/// </summary>
		/// <param name="ip">IP-адрес устройства</param>
		/// <param name="username">Логин</param>
		/// <param name="password">Пароль</param>
		public MicrotikClient(string ip, string login, string password)
		{
			Ip = ip;
			Login = login;
			Password = password;
		}

		/// <summary>
		/// Выполняет удаление IP адреса из списка адресов
		/// </summary>
		/// <param name="ipAddress">Удаляемый адрес</param>
		/// <param name="addressListName">Название списка</param>
		/// <returns>Результат выполнения команд</returns>
		public IEnumerable<ITikSentence> RemoveIpAddressFromList(string ipAddress, string addressListName)
		{
			string[] commands = new string[] {
				"/ip/firewall/address-list/print",
				string.Format("?list={0}", addressListName),
				//"=.proplist=address"
            };

			var result = Connection.CallCommandSync(commands);
			return result;
			}

		/// <summary>
		/// Создает правилол проброса портов
		/// </summary>
		/// <param name="ipAddress">IP адрес компьютера внутри сети</param>
		/// <param name="internalPort">Внутренний порт</param>
		/// <param name="externalPort">Внешний порт</param>
		/// <param name="comment">Коментарий к создаваемому правилу</param>
		/// <returns></returns>
		public IEnumerable<ITikSentence> OpenPort(string ipAddress, string internalPort, string externalPort, string comment = default)
		{
			string[] commands = new string[] {
				"/ip/firewall/nat/add",
				"=chain=dstnat",
				"=action=dst-nat",
				"=in-interface=ether1-gateway",
				"=protocol=tcp",
				string.Format("=dst-port={0}", externalPort),
				string.Format("=to-addresses={0}", ipAddress),
				string.Format("=to-ports={0}", internalPort),
				string.Format("=comment={0}", comment)
			};

			var result = Connection.CallCommandSync(commands);
			return result;
		}

		/// <summary>
		/// Формирует строку - результат выполнения команд
		/// </summary>
		/// <param name="result"></param>
		public string Print(IEnumerable<ITikSentence> commandsExecutionResult)
		{
			string resultString = default;


			foreach (ITikSentence sentence in commandsExecutionResult)
			{
				if (sentence is ITikTrapSentence)
					resultString += string.Format("Сообщение об ошибке: {0}\n", ((ITikTrapSentence)sentence).Message);
				else if (sentence is ITikDoneSentence)
					resultString += "Команда выполнена\n";
				else if (sentence is ITikReSentence)
				{

					foreach (var wordPair in sentence.Words)
					{
						resultString += string.Format("  {0} = {1}\n", wordPair.Key, wordPair.Value);
					}
				}
				else
				{
					throw new NotImplementedException("Неизвестная команда");
			}
		}

			return resultString;
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

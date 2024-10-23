using System.Net.Sockets;
using System.Text;
using System.Text.Json;


namespace MessageSender
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("Укажите адрес сервера (например, 127.0.0.1):");
			string server = Console.ReadLine();

			Console.WriteLine("Укажите порт (например, 8080):");
			if (!int.TryParse(Console.ReadLine(), out int port))
			{
				Console.WriteLine("Некорректный ввод порта.");
				return;
			}

			while (true)
			{
				// Запрашиваем количество сообщений
				Console.WriteLine("Укажите количество сообщений для отправки:");
				if (!int.TryParse(Console.ReadLine(), out int messageCount))
				{
					Console.WriteLine("Некорректный ввод количества сообщений.");
					return;
				}

				// Создаем массив сообщений заранее
				var messages = CreateMessages(messageCount);

				// Вывод времени начала отправки
				DateTime startTime = DateTime.Now;
				Console.WriteLine($"[{startTime:yyyy-MM-dd HH:mm:ss.fff}] Начало отправки {messageCount} сообщений...");

				int batchSize = 1; // Количество сообщений на один поток
				int threadCount = Math.Min(1000000, messageCount / batchSize); // Количество потоков
				await ParallelSendMessages(server, port, messages, threadCount);

				// Вывод времени окончания отправки
				DateTime endTime = DateTime.Now;
				Console.WriteLine($"[{endTime:yyyy-MM-dd HH:mm:ss.fff}] Отправка {messageCount} сообщений завершена.");
				TimeSpan duration = endTime - startTime;
				Console.WriteLine($"Отправка {messageCount} сообщений заняла {duration.TotalMilliseconds} миллисекунд.");

				// Спрашиваем, нужно ли отправить еще
				Console.WriteLine("Отправить еще сообщения? (y/n):");
				string response = Console.ReadLine().ToLower();
				if (response != "y")
				{
					break;
				}
			}
		}

		// Метод для создания сообщений
		static MessageData[] CreateMessages(int messageCount)
		{
			var messages = new MessageData[messageCount];

			for (int i = 0; i < messageCount; i++)
			{
				// Создаем JSON для каждого сообщения
				var messageContent = new
				{
					PlayerId = $"player{i % 1000}",
					Value1 = $"example_{i}",
					Value2 = $"example_{i}"
				};

				string messageJson = JsonSerializer.Serialize(messageContent);

				messages[i] = new MessageData
				{
					Id = i, // Присваиваем уникальный ID
					MessageJson = messageJson // Сохраняем JSON сообщение
				};
			}

			return messages;
		}

		// Параллельная отправка сообщений
		static async Task ParallelSendMessages(string server, int port, MessageData[] messages, int threadCount)
		{
			var tasks = new Task[threadCount];
			int messagesPerThread = messages.Length / threadCount;

			for (int i = 0; i < threadCount; i++)
			{
				// Разделяем отправку сообщений между потоками
				int startIndex = i * messagesPerThread;
				int endIndex = (i == threadCount - 1) ? messages.Length : startIndex + messagesPerThread;

				tasks[i] = Task.Run(() => SendMessages(server, port, messages, startIndex, endIndex));
			}

			await Task.WhenAll(tasks);
		}

		// Отправка сообщений
		// Метод для отправки сообщений с получением ответа от сервера
		static async Task SendMessages(string server, int port, MessageData[] messages, int startIndex, int endIndex)
		{
			using (TcpClient client = new TcpClient(server, port))
			{
				NetworkStream stream = client.GetStream();

				for (int i = startIndex; i < endIndex; i++)
				{
					var messageData = messages[i];

					// Преобразуем строку JSON в байты
					byte[] data = Encoding.UTF8.GetBytes(messageData.MessageJson);

					// Отправляем данные
					DateTime sendTime = DateTime.Now; // Время отправки сообщения
					await stream.WriteAsync(data, 0, data.Length);

					// Получаем ответ от сервера
					byte[] responseBuffer = new byte[1024];
					int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
					string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

					DateTime receiveTime = DateTime.Now; // Время получения ответа
					TimeSpan roundTripTime = receiveTime - sendTime; // Время, затраченное на отправку и получение ответа

					Console.WriteLine($"[{receiveTime:yyyy-MM-dd HH:mm:ss.fff}] Ответ получен: {response}");
					Console.WriteLine($"Задержка для сообщения {i}: {roundTripTime.TotalMilliseconds} миллисекунд.");
				}
			}
		}
	}

	// Класс для хранения данных сообщения
	class MessageData
	{
		public int Id { get; set; } // Уникальный идентификатор сообщения
		public string MessageJson { get; set; } // Строка JSON сообщения
	}
}
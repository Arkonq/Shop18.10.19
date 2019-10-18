using Microsoft.Extensions.Configuration;
using Shop.DataAccess;
using Shop.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/*
		1. Регистрация и вход (смс-код / email код) - сделать до 11.10 (Email есть на метаните)
		2. История покупок 
		3. Категории и товары (картинка в файловой системе)
		4. Покупка (корзина), оплата и доставка (PayPal/Qiwi/etc)
		5. Комментарии и рейтинги
		6. Поиск (пагинация - постраничность)

		Кто сделает 3 версии (Подключенный, автономный и EF) получит автомат на экзамене
*/

namespace Shop.UI
{
	class Program
	{
		static IConfigurationBuilder builder = new ConfigurationBuilder()
							.SetBasePath(Directory.GetCurrentDirectory())
							.AddJsonFile("appsettings.json", false, true);

		static IConfigurationRoot configurationRoot = builder.Build();
		static string connectionString = configurationRoot.GetConnectionString("HomeConnectionString");
		static string providerName = configurationRoot
							.GetSection("AppConfig")
							.GetChildren().Single(item => item.Key == "ProviderName")
							.Value;

		static void Main(string[] args)
		{
			Search();
			//Pagination();
			//Test();
			//Registration();
			//SignIn();
		}

		static void ProcessCollections()
		{
			List<string> cityNames = new List<string>
			{
				"Almaty", "Ankara", "Boriswill", "Nur-Sultan", "Yalta"
			};

			List<string> processedCityNames = new List<string>(); // для поиска товаров от пользователя
			foreach (string name in cityNames)
			{
				if (name.Contains("-"))
				{
					processedCityNames.Add(name);
				}
			}

			var result = from name
									 in cityNames
									 where name.Contains("-")
									 select name;

			var shortResult = cityNames.Where(name => name.Contains("-"));
			var shortResult2 = cityNames.Select(name => name.Contains("-"));
		}

		private static void Test()
		{
			Category category = new Category
			{
				Name = "Бытовая техника",
				//ImagePath = "C:/data",
			};


			Item item = new Item
			{
				Name = "Пылесос",
				//ImagePath = "C:/data/items",
				//Price = 25999,
				//Description = "Обычный пылесос",
				CategoryId = category.Id
			};

			User user = new User
			{
				FullName = "Иван Иванов",
				PhoneNumber = "123456",
				Email = "qwer@qwr.qwr",
				Address = "Twesd, 12",
				Password = "password",
				VerificationCode = "1234"
			};

			using (var context = new ShopContext(connectionString))
			{
				//context.Users.Add(user);

				context.Items.Add(item);

				//context.Categories.Add(category);

				var result = context.Categories.ToList();
				//context.Remove(category);

				//var quariedCategories = context.Categories.Where(x => x.CreationDate.Date < new System.DateTime(2017,10,5).Date);

				//var funnyResult = quariedCategories.Select(x => new
				//{
				//	Id = x.Id,
				//	StartDate = x.CreationDate,
				//	FunnyName = "Funny" + x.Name
				//});

				//var finalResult = funnyResult.ToList();

				context.SaveChanges();
			}

			string data = "12345sdf";
			var newString = data.ExtractOnlyText();
		}


		static void Registration()
		{
			string fullName, phoneNum, email, address, password, verCode;
			Console.WriteLine("Введите ФИО: ");
			fullName = Console.ReadLine();
			Console.WriteLine("Введите почту: ");
			email = Console.ReadLine();
			Console.WriteLine("Введите номер телефона: ");
			phoneNum = Console.ReadLine();
			Console.WriteLine("Введите адрес: ");
			address = Console.ReadLine();
			Console.WriteLine("Введите пароль: ");
			password = Console.ReadLine();
			Console.WriteLine("Введите секретный код (****): ");
			verCode = Console.ReadLine();

			User user = new User
			{
				PhoneNumber = phoneNum,
				Email = email,
				Address = address,
				Password = password,
				VerificationCode = verCode
			};
			using (var context = new ShopContext(connectionString))
			{
				context.Users.Add(user);
				context.SaveChanges();
			}
		}
		static void SignIn()
		{
			string email, password;
			Console.WriteLine("Введите почту: ");
			email = Console.ReadLine();
			Console.WriteLine("Введите пароль: ");
			password = Console.ReadLine();
			using (var context = new ShopContext(connectionString))
			{
				var user = from u in context.Users
									 where u.Email.Equals(email) & u.Password.Equals(password)
									 select u;
				//Console.WriteLine(user.Count())
				if (user.Count() == 1)
				{
					Console.WriteLine("Введите секретный код: ");
				}
			}
		}
		static void Search()
		{
			int page = 1, pageSize = 3, pages = 1, items = 0, answ, nItem;
			string search = null;
			List<Item> result = null;
			IQueryable<Item> query;
			while (search != "0")
			{
				page = 1; // Есть вероятность что пользователь попадет в false в Int32.TryParse параметр out page, где page присваивается -1, что вскоре приведет к ошибке
				Console.Clear();
				Console.WriteLine("Введите искомый товар (0 - Назад):");
				search = Console.ReadLine();
				if (search == "0")
				{
					continue;
				}
				using (var context = new ShopContext(connectionString))
				{
					query = from item in context.Items
									where item.Name.Contains(search)
									select item;
					items = query.Count();
				}
				if (items == 0)
				{
					Console.WriteLine("По вашему запросу не найдено ни одного товара.");
					Console.ReadLine();
					continue;
				}
				pages = items / pageSize;
				if (items % pageSize != 0) // Если кол-во страниц выпало как 5/3 то выйдет лишь 1 страница, поэтому добавляем еще одну
				{
					pages++;
				}
				ShopPage(page, pageSize, pages, search, result, query);

				while (page != 0)
				{
					Console.WriteLine("0. Вернуться к поиску товаров.");
					Console.WriteLine("1. Выбрать товар.");
					Console.WriteLine("2. Выбрать страницу.");
					if (Int32.TryParse(Console.ReadLine(), out answ) == false)
					{
						Console.Clear();
						Console.WriteLine("Введенное действие не является корректным. Выберите действие из списка!");
						answ = -1;  // При выпадении false в Int32.TryParse параметр out page присваивается значние 0, и с новым циклом программа завершает работу
						Console.ReadLine();
						Console.Clear();
						continue;
					}
					switch (answ)
					{
						case 0:
							page = 0;
							continue;
						case 1:
							Console.WriteLine($"Введите товар (1 - {pageSize}):");
							if (Int32.TryParse(Console.ReadLine(), out nItem) == false || nItem == 0)
							{
								Console.Clear();
								Console.WriteLine("Введенный вариант не является корректным. Возврат в выбор товара");
								Console.ReadLine();
								Console.Clear();
								continue;
							}
							ChooseItem(nItem,page, pageSize, search, out result, out query);
							Console.WriteLine("0. Назад");
							Console.WriteLine("1. Посмотреть комментарии");
							Console.WriteLine("2. Приобрести товар");
							Console.ReadLine();
							Console.Clear();
							break;
						case 2:
							Console.WriteLine($"Введите страницу (1 - {pages}):");
							if (Int32.TryParse(Console.ReadLine(), out page) == false)
							{
								Console.Clear();
								Console.WriteLine("Введенная страница не является числом.");
								page = -1;  // При выпадении false в Int32.TryParse параметр out page присваивается значние 0, и с новым циклом программа завершает работу
								Console.ReadLine();
								Console.Clear();
								continue;
							}
							ShopPage(page, pageSize, pages, search, result, query);
							break;
					}

				}
			}
		}

		private static void ChooseItem(int toSkip,int page, int pageSize, string search, out List<Item> result, out IQueryable<Item> query)
		{
			Console.Clear();
			toSkip--;	// Если мы берем 1ый Айтем, то нужно сделать 0 скипов на странице, так как сразу берется(Take) первый же, и так далее
			using (var context = new ShopContext(connectionString))
			{
				query = from item in context.Items
								where item.Name.Contains(search)
								select item;
				var iteming = query.Skip((page - 1) * pageSize).Take(pageSize).Skip(toSkip).Take(1);
				result = iteming.ToList();
			}
			foreach (var item in result)
			{
				Console.WriteLine($"\tНаименование: {item.Name}");
				Console.WriteLine($"\tИзображение: {item.ImagePath}");
				Console.WriteLine($"\tУникальный идентификатор: {item.Id}");
				Console.WriteLine($"\tЦена: {item.Price}");
				Console.WriteLine($"\tОписание: {item.Description}");
				Console.WriteLine($"\tКомментарии - (кол-во комментов)");
			}
		}

		private static void ShopPage(int page, int pageSize, int pages, string search, List<Item> result, IQueryable<Item> query)
		{
			Console.Clear();
			if (page < 0)
			{
				page = -page;
			}
			if (page > pages)
			{
				Console.WriteLine("Введенной страницы не существует.");
				Console.ReadLine();
				Console.Clear();
				return;
			}
			using (var context = new ShopContext(connectionString))
			{
				query = from item in context.Items
								where item.Name.Contains(search)
								select item;
				var paging = query.Skip((page - 1) * pageSize).Take(pageSize);
				result = paging.ToList();
			}
			Console.WriteLine($"Page {page}/{pages}:");
			int num = 1;
			foreach (var item in result)
			{
				Console.WriteLine($"\t{num++}) {item.Name} - {item.Id}");
			}
		}

		static void Pagination()
		{
			int page = 0, pageSize = 3;
			List<Item> result;
			while (page != 0)
			{
				Console.WriteLine("Введите страницу (0 для выхода):");

				if (!Int32.TryParse(Console.ReadLine(), out page))
				{
					Console.WriteLine("Введенная страница не является числом.");
					Console.ReadLine();
					Console.Clear();
					continue;
				}
				Console.Clear();
				using (var context = new ShopContext(connectionString))
				{
					var query = from item in context.Items
											orderby item.Name
											select item;

					var paging = query.Skip((page - 1) * pageSize).Take(pageSize);

					result = paging.ToList();
				}
				Console.WriteLine($"Page {page}:");
				foreach (var item in result)
				{
					Console.WriteLine($"\t{item.Name} - {item.Price} тг");
				}
			}
		}
	}
}
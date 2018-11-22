using hobbyTrackerAPI.Controllers;
using hobbyTrackerAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace unitTestHobbyTrackerAPI
{
	[TestClass]
	public class PutUnitTests
	{
		// Insert your code here
		public static readonly DbContextOptions<hobbyTrackerAPIContext> options
= new DbContextOptionsBuilder<hobbyTrackerAPIContext>()
.UseInMemoryDatabase(databaseName: "testDatabase")
.Options;
		public static IConfiguration configuration = null;
		public static readonly IList<string> memeTitles = new List<string> { "dankMeme", "dankerMeme" };

		[TestInitialize]
		public void SetupDb()
		{
			using (var context = new hobbyTrackerAPIContext(options))
			{
				HobbyItem hobbyItem1 = new HobbyItem()
				{
					Title = memeTitles[0]
				};

				HobbyItem hobbyItem2 = new HobbyItem()
				{
					Title = memeTitles[1]
				};

				context.HobbyItem.Add(hobbyItem1);
				context.HobbyItem.Add(hobbyItem2);
				context.SaveChanges();
			}
		}

		[TestCleanup]
		public void ClearDb()
		{
			using (var context = new hobbyTrackerAPIContext(options))
			{
				context.HobbyItem.RemoveRange(context.HobbyItem);
				context.SaveChanges();
			};
		}
	}
}

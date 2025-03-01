using BIGMVC_project.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BIGMVC_project.Controllers
{
	public class ManagerController : Controller
	{
		private readonly MyDbContext _context;
		public ManagerController(MyDbContext context)
		{
			_context = context;
		}
		public IActionResult Index()
		{
			return View();
		}
		public IActionResult Addemployee()
		{
			return View();
		}
		[HttpPost]
		public IActionResult Addemployee(Employee employees)
		{
			if (ModelState.IsValid)
			{
				_context.Add(employees);
				_context.SaveChanges();
				return View(); //RedirectToAction("Index")
			}
			return View();
		}
		[HttpGet]
		public IActionResult LeavingR()
		{
			var data = _context.LeaveRequests.ToList();
			return View(data);
		}
		[HttpPost]
		public IActionResult LeavingR(int id, string status)
		{
			var findd = _context.LeaveRequests.Find(id);
			if (findd != null)
			{
				findd.LeaveRequestsStatusEnum = status;
				_context.LeaveRequests.Update(findd);
				_context.SaveChanges();
			}
			return RedirectToAction("LeavingR");
		}
		public IActionResult contact()
		{
			return View();
		}
		[HttpPost]
		public IActionResult contact(Feedback feedbacks)
		{
			if (ModelState.IsValid)
			{
				_context.Add(feedbacks);
				_context.SaveChanges();
				return View();
			}
			return View();
		}

		public IActionResult About_us()
		{
			return View();
		}
		public IActionResult Show_Employee()
		{
			var idd = HttpContext.Session.GetInt32("Id");
			var getemp = _context.Employees.Where(e => e.Id == idd).ToList();
			return View(getemp);
		}
		public IActionResult Questions()
		{
			return View();
		}
		[HttpPost]
		public IActionResult Questions(IFormCollection form, int id)
		{
			// Process the form data
			int q1 = int.Parse(form["q1"]);
			int q2 = int.Parse(form["q2"]);
			int q3 = int.Parse(form["q3"]);
			int q4 = int.Parse(form["q4"]);
			int q5 = int.Parse(form["q5"]);
			int q6 = int.Parse(form["q6"]);
			int q7 = int.Parse(form["q7"]);
			int q8 = int.Parse(form["q8"]);
			int q9 = int.Parse(form["q9"]);
			int q10 = int.Parse(form["q10"]);

			int totalScore = q1 + q2 + q3 + q4 + q5 + q6 + q7 + q8 + q9 + q10;
			totalScore = Math.Max(0, Math.Min(10, totalScore));

			// Determine evaluation result
			string result;
			if (totalScore >= 7)
			{
				result = "excellent";
			}
			else if (totalScore >= 4)
			{
				result = "good";
			}
			else
			{
				result = "bad";
			}

			// Update the evaluation result in the database
			var updateEvaluation = _context.Evaluations.Find(id);
			if (updateEvaluation != null)
			{
				updateEvaluation.EvaluationsStatusEnum = result; // Assuming the column name is "EvaluationResult"
				_context.Evaluations.Update(updateEvaluation);
				_context.SaveChanges();
			}

			// Pass the result to the view
			ViewBag.TotalScore = totalScore;

			return View("Addemployee");
		}

	}


}


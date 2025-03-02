using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BIGMVC_project.Models;
using System.Xml.Linq;
//using HR_Management.Data;
//using HR_Management.Model;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HR_Management.Controllers
{
	public class HrsController : Controller
	{
		private readonly MyDbContext _context;

		public HrsController(MyDbContext context)
		{
			_context = context;
		}
		public IActionResult Dashboard()
		{
			// إجمالي الموظفين
			//ViewBag.TotalEmployees = _context.Employees.Count();


			return View();
		}


		public IActionResult LeaveRequests()
		{
			var allLeaveRequests = _context.LeaveRequests.ToList();
			return View(allLeaveRequests);
		}

		public IActionResult Feedback() // there is an error here replay message was not found
		{
			var feedback = _context.Feedbacks.ToList();
			return View(feedback);
		}
		[HttpPost]
		public async Task<IActionResult> SubmitFeedback(Feedback feedback)
		{
			if (ModelState.IsValid)
			{
				feedback.SubmittedAt = DateTime.Now;
				_context.Feedbacks.Add(feedback);
				await _context.SaveChangesAsync();
				TempData["Success"] = "Your feedback has been submitted!";
				return RedirectToAction("Feedback");
			}

			return View("Feedback", feedback);
		}

		[HttpPost]
		public async Task<IActionResult> DeleteFeedback(int id)
		{
			var feedback = await _context.Feedbacks.FindAsync(id);
			if (feedback != null)
			{
				_context.Feedbacks.Remove(feedback);
				await _context.SaveChangesAsync();
			}

			return RedirectToAction("Feedback");
		}
		[HttpPost]
		public IActionResult ReplyToFeedback(int id, string reply)
		{
			var feedback = _context.Feedbacks.Find(id);
			if (feedback == null)
			{
				return NotFound();
			}

			//feedback.ReplyMessage = reply;
			_context.SaveChanges();

			return RedirectToAction("Feedback");
		}

		//public IActionResult ContactUs()
		//{
		//	return View(new Feedback());
		//}

		[HttpPost]
		public IActionResult SendFeedback(Feedback feedback)
		{
			if (ModelState.IsValid)
			{
				feedback.SubmittedAt = DateTime.Now;
				_context.Feedbacks.Add(feedback);
				_context.SaveChanges();
				return RedirectToAction("ContactUs");
			}

			return View("ContactUs", feedback);
		}


		public IActionResult AddManager()
		{
			var departments = _context.Departments.ToList(); // assuming you're using Entity Framework
			ViewBag.Departments = departments;
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> AddManager(Manager model, IFormFile profilePicture)
		{
			if (_context.Managers.Any(m => m.Email == model.Email))
			{
				ModelState.AddModelError("Email", "This email is already in use.");
				return View(model);
			}

			// Check if the model is valid
			if (!ModelState.IsValid)
			{
				return View(model); // Return the view with validation errors if the model is invalid
			}

			if (profilePicture != null && profilePicture.Length > 0)
			{
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
				var fileExtension = Path.GetExtension(profilePicture.FileName).ToLower();
				if (!allowedExtensions.Contains(fileExtension))
				{
					ModelState.AddModelError("ProfilePicture", "Invalid image format. Only .jpg, .jpeg, and .png are allowed.");
					return View(model);
				}

				var fileName = Path.GetFileName(profilePicture.FileName);
				var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "managers", fileName);
				var directoryPath = Path.GetDirectoryName(filePath);

				if (!Directory.Exists(directoryPath))
				{
					Directory.CreateDirectory(directoryPath);
				}

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await profilePicture.CopyToAsync(stream);
				}

				model.Image = fileName;
			}

			_context.Managers.Add(model);
			await _context.SaveChangesAsync();

			// Redirect to a confirmation page or show success
			return RedirectToAction("ManagerDetails", new { id = model.Id }); // Adjust to a relevant action
		}

		// GET: Hrs/ManagerDetails/5
		[HttpGet("manager/details/{id}")]
		public IActionResult ManagerDetails(int id)
		{
			var manager = _context.Managers
				.Include(m => m.Department)
				.FirstOrDefault(m => m.Id == id);

			if (manager == null)
			{
				return NotFound();
			}

			return View(manager);
		}
		public IActionResult ExportLeaveRequestsToPDF()
		{
			var leaveRequests = _context.LeaveRequests.ToList();

			var pdfStream = new MemoryStream();

			var document = new Document(PageSize.A4);
			var writer = PdfWriter.GetInstance(document, pdfStream);

			document.Open();

			var font = FontFactory.GetFont("Arial", 20, Font.BOLD);
			Paragraph title = new Paragraph("Leave Requests", font)
			{
				Alignment = Element.ALIGN_CENTER
			};

			title.SpacingAfter = 20f;

			document.Add(title);

			document.Add(new Paragraph(" "));

			var table = new PdfPTable(5) { WidthPercentage = 100 };
			table.SpacingBefore = 20f;

			table.AddCell("Employee ID");
			table.AddCell("Start Date");
			table.AddCell("End Date");
			table.AddCell("Reason");
			table.AddCell("Status");

			foreach (var request in leaveRequests)
			{
				table.AddCell(request.EmployeeId.ToString());
				table.AddCell(request.StartDate.ToShortDateString());
				table.AddCell(request.EndDate.ToShortDateString());
				table.AddCell(request.Reason);
				table.AddCell(request.LeaveRequestsStatusEnum);
			}

			document.Add(table);

			document.Close();

			return File(pdfStream.ToArray(), "application/pdf", "LeaveRequests.pdf");
		}

		public IActionResult DepartmentService()
		{
			return View();
		}
		//////////////////////////////////////
		/////////////////////////////////////////////
		/////////////////////////////////////////////
		///// GET: Hrs
		public async Task<IActionResult> Index()
		{
			return View(await _context.Hrs.ToListAsync());
		}

		// GET: Hrs/Details/5
		public async Task<IActionResult> HRDetails(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var hr = await _context.Hrs
				.FirstOrDefaultAsync(m => m.Id == id);
			if (hr == null)
			{
				return NotFound();
			}

			return View(hr);
		}




		public IActionResult ExportHRToPDF()
		{
			var HR = _context.Hrs.ToList();

			var pdfStream = new MemoryStream();
			var document = new Document(PageSize.A4);
			var writer = PdfWriter.GetInstance(document, pdfStream);

			document.Open();

			var font = FontFactory.GetFont("Arial", 20, Font.BOLD);
			Paragraph title = new Paragraph("HR Team", font)
			{
				Alignment = Element.ALIGN_CENTER
			};

			title.SpacingAfter = 20f;

			document.Add(title);
			document.Add(new Paragraph(" "));

			var table = new PdfPTable(3) { WidthPercentage = 100 }; // خليتها 3 لأنك بتعرض (ID - Name - Email)
			table.SpacingBefore = 20f;

			// Header
			table.AddCell("ID");
			table.AddCell("Name");
			table.AddCell("Email");

			foreach (var request in HR)
			{
				table.AddCell(request.Id.ToString());
				table.AddCell(request.Name ?? "N/A");
				table.AddCell(request.Email ?? "N/A");
			}

			document.Add(table);
			document.Close();

			// هنا إعادة تعيين Position بتاعة MemoryStream 🔥
			pdfStream.Position = 0;

			return File(pdfStream.ToArray(), "application/pdf", "HRteam.pdf");
		}

		///////////////////////////////-----------------------------------------////////////////////////////////////
		///////////////////////////////-----------------------------------------////////////////////////////////////
		///////////////////////////////-----------------------------------------////////////////////////////////////

		public async Task<IActionResult> Department()
		{
			return View(await _context.Departments.ToListAsync());
		}

		// GET: Departments/Details/5
		public async Task<IActionResult> DetailsDepartment(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var department = await _context.Departments
				.FirstOrDefaultAsync(m => m.Id == id);
			if (department == null)
			{
				return NotFound();
			}

			return View(department);
		}
		// GET: Departments/Create
		public IActionResult DepartmentCreate()
		{
			return View();
		}

		// POST: Departments/Create
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DepartmentCreate([Bind("Id,Name,Description")] Department department)
		{
			if (ModelState.IsValid)
			{
				_context.Add(department);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Department));
			}
			return View(department);
		}


		public IActionResult DeparmentToPDF()
		{
			var departments = _context.Departments.ToList();

			var pdfStream = new MemoryStream();
			var document = new Document(PageSize.A4);
			var writer = PdfWriter.GetInstance(document, pdfStream);

			document.Open();

			var font = FontFactory.GetFont("Arial", 20, Font.BOLD);
			Paragraph title = new Paragraph("Departments", font)
			{
				Alignment = Element.ALIGN_CENTER
			};

			title.SpacingAfter = 20f;

			document.Add(title);
			document.Add(new Paragraph(" "));

			var table = new PdfPTable(3) { WidthPercentage = 100 }; // خليتها 3 لأنك بتعرض (ID - Name - Email)
			table.SpacingBefore = 20f;

			// Header
			table.AddCell("ID");
			table.AddCell("Name");
			table.AddCell("Description");

			foreach (var request in departments)
			{
				table.AddCell(request.Id.ToString());
				table.AddCell(request.Name ?? "N/A");
				table.AddCell(request.Description ?? "N/A");
			}

			document.Add(table);
			document.Close();

			// هنا إعادة تعيين Position بتاعة MemoryStream 🔥
			pdfStream.Position = 0;

			return File(pdfStream.ToArray(), "application/pdf", "Departments.pdf");
		}




		// GET: Employees/View
		public async Task<IActionResult> Employee()
		{
			var myDbContext = _context.Employees.Include(e => e.Department).Include(e => e.Manager);
			return View(await myDbContext.ToListAsync());
		}

		// GET: Employees/Details/5
		public async Task<IActionResult> EmployeeDetails(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var employee = await _context.Employees
				.Include(e => e.Department)
				.Include(e => e.Manager)
				.FirstOrDefaultAsync(m => m.Id == id);
			if (employee == null)
			{
				return NotFound();
			}

			return View(employee);
		}



		public IActionResult EmployeeToPDF()
		{
			var employees = _context.Employees
				.Include(e => e.Department)  // تضمين القسم
				.Include(e => e.Manager)     // تضمين المدير
			.ToList();

			var pdfStream = new MemoryStream();
			var document = new Document(PageSize.A4);
			var writer = PdfWriter.GetInstance(document, pdfStream);

			document.Open();

			var font = FontFactory.GetFont("Arial", 20, Font.BOLD);
			Paragraph title = new Paragraph("Our Employees", font)
			{
				Alignment = Element.ALIGN_CENTER
			};

			title.SpacingAfter = 20f;

			document.Add(title);
			document.Add(new Paragraph(" "));

			var table = new PdfPTable(6) { WidthPercentage = 100 }; // تعديل هنا لجعلها 7 أعمدة
			table.SpacingBefore = 20f;

			// Header
			table.AddCell("Name");
			table.AddCell("Email");
			table.AddCell("Address");
			table.AddCell("Position");
			table.AddCell("Manager");
			table.AddCell("Department Name");

			foreach (var employee in employees)
			{
				table.AddCell(employee.Name ?? "N/A");
				table.AddCell(employee.Email ?? "N/A");
				table.AddCell(employee.Address ?? "N/A");
				table.AddCell(employee.Position ?? "N/A");
				table.AddCell(employee.Manager?.Name ?? "N/A");
				table.AddCell(employee.Department?.Name ?? "N/A");
			}

			document.Add(table);
			document.Close();

			// إعادة تعيين Position بتاعة MemoryStream
			pdfStream.Position = 0;

			return File(pdfStream.ToArray(), "application/pdf", "Employees.pdf");
		}



		// GET: Employees/Create
		public IActionResult EmployeeCreate()
		{
			// جلب قائمة الأقسام وتمريرها إلى ViewBag
			ViewBag.DepartmentId = new SelectList(_context.Departments, "Id", "Name");

			// جلب قائمة المديرين وتمريرها إلى ViewBag (إذا كان هناك مديرون)
			ViewBag.ManagerId = new SelectList(_context.Employees, "Id", "Name");

			return View();
		}



		// POST: Employees/Create
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EmployeeCreate([Bind("Id,Name,Email,PasswordHash,ImagePath,Address,Position,ManagerId,DepartmentId")] Employee employee)
		{
			if (ModelState.IsValid)
			{
				_context.Add(employee);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Employee));
			}
			ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Id", employee.DepartmentId);
			ViewData["ManagerId"] = new SelectList(_context.Managers, "Id", "Id", employee.ManagerId);
			return View(employee);
		}



		// GET: Managers
		public async Task<IActionResult> Manager()
		{
			var myDbContext = _context.Managers.Include(m => m.Department);
			return View(await myDbContext.ToListAsync());
		}

		// MANAGER TO PDF
		public IActionResult ManagerToPDF()
		{
			var managers = _context.Managers
				.Include(m => m.Department)  // تضمين القسم المرتبط بكل مدير
			.ToList();

			var pdfStream = new MemoryStream();
			var document = new Document(PageSize.A4);
			var writer = PdfWriter.GetInstance(document, pdfStream);

			document.Open();

			var font = FontFactory.GetFont("Arial", 20, Font.BOLD);
			Paragraph title = new Paragraph("Our Managers", font)
			{
				Alignment = Element.ALIGN_CENTER
			};

			title.SpacingAfter = 20f;

			document.Add(title);
			document.Add(new Paragraph(" "));

			var table = new PdfPTable(3) { WidthPercentage = 100 }; // نعدل هنا لأننا نعرض 3 أعمدة
			table.SpacingBefore = 20f;

			// Header
			table.AddCell("Name");
			table.AddCell("Email");
			table.AddCell("Department Name");

			foreach (var manager in managers)
			{
				table.AddCell(manager.Name ?? "N/A");
				table.AddCell(manager.Email ?? "N/A");
				table.AddCell(manager.Department?.Name ?? "N/A");  // عرض اسم القسم إذا كان موجودًا
			}

			document.Add(table);
			document.Close();

			// إعادة تعيين Position بتاعة MemoryStream
			pdfStream.Position = 0;

			return File(pdfStream.ToArray(), "application/pdf", "Managers.pdf");
		}

		// GET: Managers/Details/5
		public async Task<IActionResult> ManagerDetails(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var manager = await _context.Managers
				.Include(m => m.Department)
				.FirstOrDefaultAsync(m => m.Id == id);
			if (manager == null)
			{
				return NotFound();
			}

			return View(manager);
		}

		//// GET: Managers/Create
		//public IActionResult ManagerCreate()
		//{
		//	ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Id");
		//	return View();
		//}

		//// POST: Managers/Create
		//// To protect from overposting attacks, enable the specific properties you want to bind to.
		//// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		//[HttpPost]
		//[ValidateAntiForgeryToken]
		//public async Task<IActionResult> ManagerCreate([Bind("Id,Name,Email,PasswordHash,DepartmentId,Image")] Manager manager)
		//{
		//	if (ModelState.IsValid)
		//	{
		//		_context.Add(manager);
		//		await _context.SaveChangesAsync();
		//		return RedirectToAction(nameof(Manager));
		//	}
		//	ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Id", manager.DepartmentId);
		//	return View(manager);
		//}




		// GET: Evaluations
		public async Task<IActionResult> Evaluation()
		{
			var myDbContext = _context.Evaluations.Include(e => e.Employee).Include(e => e.Manager);
			return View(await myDbContext.ToListAsync());
		}

		// GET: Evaluations/Details/5
		public async Task<IActionResult> EvaluationDetails(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var evaluation = await _context.Evaluations
				.Include(e => e.Employee)
				.Include(e => e.Manager)
				.FirstOrDefaultAsync(m => m.Id == id);
			if (evaluation == null)
			{
				return NotFound();
			}

			return View(evaluation);
		}

		public IActionResult EvaluationToPDF()
		{
			var evaluations = _context.Evaluations
				.Include(e => e.Employee)  // تضمين الموظف المرتبط بالتقييم
				.Include(e => e.Manager)   // تضمين المدير المرتبط بالتقييم
			.ToList();

			var pdfStream = new MemoryStream();
			var document = new Document(PageSize.A4);
			var writer = PdfWriter.GetInstance(document, pdfStream);

			document.Open();

			var font = FontFactory.GetFont("Arial", 20, Font.BOLD);
			Paragraph title = new Paragraph("Employee Evaluations", font)
			{
				Alignment = Element.ALIGN_CENTER
			};

			title.SpacingAfter = 20f;

			document.Add(title);
			document.Add(new Paragraph(" "));

			var table = new PdfPTable(5) { WidthPercentage = 100 }; // 6 أعمدة: ID - Employee Name - Manager Name - Evaluation Date - Rating - Comments
			table.SpacingBefore = 20f;

			// Header
			table.AddCell("Employee Name");
			table.AddCell("Manager Name");
			table.AddCell("Evaluation Date");
			table.AddCell("Rating");
			table.AddCell("Comments");

			foreach (var evaluation in evaluations)
			{
				table.AddCell(evaluation.Employee?.Name ?? "N/A");  // اسم الموظف
				table.AddCell(evaluation.Manager?.Name ?? "N/A");   // اسم المدير
				table.AddCell(evaluation.DateEvaluated?.ToString("yyyy-MM-dd") ?? "N/A");  // تاريخ التقييم
				table.AddCell(evaluation.EvaluationsStatusEnum.ToString() ?? "N/A");  // تقييم
				table.AddCell(evaluation.Comments ?? "N/A");  // التعليقات (إذا كانت موجودة)
			}

			document.Add(table);
			document.Close();

			// إعادة تعيين Position بتاعة MemoryStream
			pdfStream.Position = 0;

			return File(pdfStream.ToArray(), "application/pdf", "Evaluations.pdf");
		}
		private bool HrExists(int id)
		{
			return _context.Hrs.Any(e => e.Id == id);
		}
	}
}


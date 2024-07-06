using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using WebApplication1.Models;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using ExcelDataReader;
using OfficeOpenXml;
using System.Net;
using System.Data.OleDb;
using System.Net.Mail;
using System.ComponentModel.DataAnnotations;
using static WebApplication1.Controllers.HomeController;
using System.Diagnostics;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {

        db_sll2DataContext tt = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True");

        public ActionResult Index()
        {
            if (Session["khach"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            return View();
        }
        public ActionResult AboutUs1()
        {
            if (Session["khach"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            return View();
        }
        public class ClassTeacherViewModel
        {
            public int ClassId { get; set; }
            public string ClassName { get; set; }
            public int? AcademicYear { get; set; }
            public int TeacherId { get; set; }
            public string TeacherName { get; set; }
        }

        public ActionResult ListTeacher(int pageNumber = 1, int pageSize = 5)
        {
            var user = Session["khach"] as User;
            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            using (var db = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                // Query to get the total count of classes
                int totalCount = db.Classes
                    .Join(db.TeacherClassAssignments, c => c.ClassId, tca => tca.ClassId, (c, tca) => new { c, tca })
                    .Join(db.Teachers, ct => ct.tca.TeacherId, t => t.TeacherId, (ct, t) => new { ct.c, t })
                    .Count();

                // Calculate the total number of pages
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Query to get the classes for the current page
                var classes = db.Classes
                    .Join(db.TeacherClassAssignments, c => c.ClassId, tca => tca.ClassId, (c, tca) => new { c, tca })
                    .Join(db.Teachers, ct => ct.tca.TeacherId, t => t.TeacherId, (ct, t) => new
                    {
                        ct.c.ClassId,
                        ct.c.ClassName,
                        ct.c.AcademicYear,
                        TeacherId = t.TeacherId,
                        TeacherName = t.Name
                    })
                    .OrderByDescending(ct => ct.AcademicYear)
                    .ThenBy(ct => ct.ClassName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(ct => new ClassTeacherViewModel
                    {
                        ClassId = ct.ClassId,
                        ClassName = ct.ClassName,
                        AcademicYear = ct.AcademicYear,
                        TeacherId = ct.TeacherId,
                        TeacherName = ct.TeacherName
                    })
                    .ToList();

                ViewBag.PageNumber = pageNumber;
                ViewBag.TotalPages = totalPages;

                return View(classes);
            }
        }

        private db_sll2DataContext db = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True");

        public class ClassInfo
        {
            public int ClassId { get; set; }
            public string ClassName { get; set; }
        }

        public class AssignHeadTeacherViewModel
        {
            public int TeacherId { get; set; }
            public int ClassId { get; set; }
            public List<ClassInfo> AvailableClasses { get; set; }
        }
        public class AssignTeachingClassViewModel
        {
            public int TeacherId { get; set; }
            public int ClassId { get; set; }
            public List<ClassInfo> AvailableClasses { get; set; }
        }


        public class TeacherDetailsViewModel
        {
            public Teacher Teacher { get; set; }
            public List<string> HeadTeacherClasses { get; set; } = new List<string>();
            public List<string> TeachingClasses { get; set; } = new List<string>();
            public List<Class> AvailableClasses { get; set; } = new List<Class>();
        }
        public class Class1
        {
            public int ClassId { get; set; }
            public string ClassName { get; set; } // Ensure this property exists
        }

        public ActionResult ListSubjects()
        {
            var user = Session["khach"] as User;
            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            var subjects = db.Subjects.ToList();
            return View(subjects);
        }


        public class Classa
        {
            public int ClassId { get; set; }
            public string ClassName { get; set; }
            public int? AcademicYear { get; set; }
        }

        public class TeacherAssignmentViewModel
        {
            public int TeacherId { get; set; }
            public string TeacherName { get; set; }
            public string Subject { get; set; }
            public string PhoneNumber { get; set; }
            public string Username { get; set; }
            public string Address { get; set; }
            public List<Classa> HomeroomClasses { get; set; }
            public List<Classa> TeachingClasses { get; set; }
            public List<Classa> AvailableHomeroomClasses { get; set; }
            public List<Classa> AvailableTeachingClasses { get; set; }
        }

        [HttpPost]
        public ActionResult AssignHomeroomTeacher(int teacherId, int classId)
        {
            try
            {
                // Debugging
                System.Diagnostics.Debug.WriteLine($"AssignHomeroomTeacher: teacherId={teacherId}, classId={classId}");

                var existingAssignment = db.TeacherClassAssignments.FirstOrDefault(tca => tca.ClassId == classId && tca.IsHeadTeacher == 1);
                if (existingAssignment == null)
                {
                    var newAssignment = new TeacherClassAssignment
                    {
                        TeacherId = teacherId,
                        ClassId = classId,
                        IsHeadTeacher = 1
                    };
                    db.TeacherClassAssignments.InsertOnSubmit(newAssignment);
                    db.SubmitChanges();
                    TempData["Message"] = "Assigned as homeroom teacher successfully!";
                }
                else
                {
                    TempData["Error"] = "This class already has a homeroom teacher.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("DetailsTeacher", new { id = teacherId });
        }
        [HttpPost]
        public ActionResult AssignTeachingClass(int teacherId, int classId)
        {
            try
            {
                // Debugging
                System.Diagnostics.Debug.WriteLine($"AssignTeachingClass: teacherId={teacherId}, classId={classId}");

                // Get the subject of the teacher
                var teacher = db.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);
                if (teacher != null)
                {
                    var subject = teacher.SubjectTC;

                    // Check if there is already a teacher assigned to this subject
                    var existingTeacherForSubject = db.TeacherClassAssignments
                                                     .Join(db.Teachers,
                                                           tca => tca.TeacherId,
                                                           t => t.TeacherId,
                                                           (tca, t) => new { tca, t })
                                                     .Where(joined => joined.t.SubjectTC == subject && joined.tca.ClassId == classId)
                                                     .FirstOrDefault();

                    if (existingTeacherForSubject != null)
                    {
                        TempData["Error"] = "Đã có giáo viên được gán dạy môn này.";
                        return RedirectToAction("DetailsTeacher", new { id = teacherId });
                    }
                }

                var existingAssignment = db.TeacherClassAssignments
                                           .FirstOrDefault(tca => tca.TeacherId == teacherId && tca.ClassId == classId && tca.IsHeadTeacher == 0);
                if (existingAssignment == null)
                {
                    var newAssignment = new TeacherClassAssignment
                    {
                        TeacherId = teacherId,
                        ClassId = classId,
                        IsHeadTeacher = 0
                    };
                    db.TeacherClassAssignments.InsertOnSubmit(newAssignment);
                    db.SubmitChanges();
                    TempData["Message"] = "Assigned to teach class successfully!";
                }
                else
                {
                    TempData["Error"] = "This teacher is already assigned to teach this class.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("DetailsTeacher", new { id = teacherId });
        }

        public ActionResult DetailsTeacher(int id)
        {
            var teacherDetails = (from t in db.Teachers
                                  join u in db.Users on t.UserId equals u.UserId
                                  where t.TeacherId == id
                                  select new TeacherAssignmentViewModel
                                  {
                                      TeacherId = t.TeacherId,
                                      TeacherName = t.Name,
                                      Subject = t.SubjectTC,
                                      PhoneNumber = t.SDT,
                                      Username = u.Username,
                                      Address = t.Address
                                  }).FirstOrDefault();

            if (teacherDetails == null)
            {
                return HttpNotFound();
            }

            var currentYear = DateTime.Now.Year;

            // Check if the teacher is already assigned as a homeroom teacher for the current year
            var isHomeroomTeacherThisYear = db.TeacherClassAssignments
                                              .Any(tca => tca.TeacherId == id && tca.IsHeadTeacher == 1 && tca.Class.AcademicYear == currentYear);

            var homeroomClasses = (from ca in db.TeacherClassAssignments
                                   join c in db.Classes on ca.ClassId equals c.ClassId
                                   where ca.TeacherId == id && ca.IsHeadTeacher == 1
                                   select new Classa
                                   {
                                       ClassId = c.ClassId,
                                       ClassName = c.ClassName,
                                       AcademicYear = c.AcademicYear
                                   }).ToList();

            var teachingClasses = (from ca in db.TeacherClassAssignments
                                   join c in db.Classes on ca.ClassId equals c.ClassId
                                   where ca.TeacherId == id && ca.IsHeadTeacher == 0
                                   select new Classa
                                   {
                                       ClassId = c.ClassId,
                                       ClassName = c.ClassName,
                                       AcademicYear = c.AcademicYear
                                   }).ToList();

            var availableHomeroomClasses = (from c in db.Classes
                                            where c.AcademicYear == currentYear &&
                                                  !db.TeacherClassAssignments.Any(tca => tca.ClassId == c.ClassId && tca.IsHeadTeacher == 1)
                                            select new Classa
                                            {
                                                ClassId = c.ClassId,
                                                ClassName = c.ClassName,
                                                AcademicYear = c.AcademicYear
                                            }).ToList();

            var availableTeachingClasses = (from c in db.Classes
                                            where c.AcademicYear == currentYear &&
                                                  !db.TeacherClassAssignments
                                                       .Join(db.Teachers, tca => tca.TeacherId, t => t.TeacherId, (tca, t) => new { tca, t })
                                                       .Any(joined => joined.t.SubjectTC == teacherDetails.Subject && joined.tca.ClassId == c.ClassId)
                                            select new Classa
                                            {
                                                ClassId = c.ClassId,
                                                ClassName = c.ClassName,
                                                AcademicYear = c.AcademicYear
                                            }).ToList();

            teacherDetails.HomeroomClasses = homeroomClasses;
            teacherDetails.TeachingClasses = teachingClasses;
            teacherDetails.AvailableHomeroomClasses = availableHomeroomClasses;
            teacherDetails.AvailableTeachingClasses = availableTeachingClasses;
            ViewBag.IsHomeroomTeacherThisYear = isHomeroomTeacherThisYear;

            return View(teacherDetails);
        }





        [HttpPost]
        public ActionResult DeleteTeacher(int id, string subjectName)
        {
            var teacher = db.Teachers.FirstOrDefault(t => t.TeacherId == id);
            if (teacher == null)
            {
                return HttpNotFound();
            }

            // Remove related assignments first
            var assignments = db.TeacherClassAssignments.Where(tca => tca.TeacherId == id);
            db.TeacherClassAssignments.DeleteAllOnSubmit(assignments);

            // Remove the teacher
            db.Teachers.DeleteOnSubmit(teacher);
            db.SubmitChanges();

            return RedirectToAction("ListTeachersBySubject", new { subjectName = subjectName });
        }
        [HttpPost]
        public ActionResult RemoveHomeroomAssignment(int teacherId, int classId)
        {
            try
            {
                var assignment = db.TeacherClassAssignments.FirstOrDefault(tca => tca.TeacherId == teacherId && tca.ClassId == classId && tca.IsHeadTeacher == 1);
                if (assignment != null)
                {
                    db.TeacherClassAssignments.DeleteOnSubmit(assignment);
                    db.SubmitChanges();
                    TempData["Message"] = "Removed homeroom assignment successfully!";
                }
                else
                {
                    TempData["Error"] = "Homeroom assignment not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("DetailsTeacher", new { id = teacherId });
        }

        [HttpPost]
        public ActionResult RemoveTeachingAssignment(int teacherId, int classId)
        {
            try
            {
                var assignment = db.TeacherClassAssignments.FirstOrDefault(tca => tca.TeacherId == teacherId && tca.ClassId == classId && tca.IsHeadTeacher == 0);
                if (assignment != null)
                {
                    db.TeacherClassAssignments.DeleteOnSubmit(assignment);
                    db.SubmitChanges();
                    TempData["Message"] = "Removed teaching assignment successfully!";
                }
                else
                {
                    TempData["Error"] = "Teaching assignment not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("DetailsTeacher", new { id = teacherId });
        }









        public ActionResult ListBoNhiem()
        {
            var user = Session["khach"] as User;
            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            var years = db.Classes
                         .Select(c => c.AcademicYear)
                         .Distinct()
                         .OrderByDescending(y => y)
                         .ToList();

            var model = new GradeDaoTaoViewModel
            {
                Years = years
            };

            return View(model);
        }

        public ActionResult ClassPrefixes1(int year)
        {
            var classPrefixes = db.Classes
                                  .Where(c => c.AcademicYear == year)
                                  .Select(c => c.ClassName.Substring(0, 2))
                                  .Distinct()
                                  .ToList();

            var model = new ClassPrefixViewModel
            {
                Year = year,
                ClassPrefixes = classPrefixes
            };

            return View(model);
        }
        public class ClassViewModel1
        {
            public string ClassName { get; set; }
            public string HomeroomTeacherName { get; set; }
        }

        public class ClassesByPrefixViewModel1
        {
            public int Year { get; set; }
            public string Prefix { get; set; }
            public List<ClassViewModel1> Classes { get; set; }
        }

        public class AddClassViewModel
        {
            public string ClassPrefix { get; set; }
            public string ClassNameSuffix { get; set; }
            public int AcademicYear { get; set; }
        }

        public ActionResult ClassesByPrefix1(int year, string prefix)
        {
            var classes = db.Classes
                            .Where(c => c.AcademicYear == year && c.ClassName.StartsWith(prefix))
                            .GroupJoin(
                                db.TeacherClassAssignments.Where(tca => tca.IsHeadTeacher == 1),
                                c => c.ClassId,
                                tca => tca.ClassId,
                                (c, tcas) => new { Class = c, TCA = tcas.FirstOrDefault() })
                            .ToList() // Materialize the query to handle nulls in memory
                            .Select(ct => new ClassViewModel1
                            {
                                ClassName = ct.Class.ClassName,
                                HomeroomTeacherName = ct.TCA != null ? db.Teachers.FirstOrDefault(t => t.TeacherId == ct.TCA.TeacherId)?.Name : "Chưa phân công"
                            })
                            .ToList();

            var model = new ClassesByPrefixViewModel1
            {
                Year = year,
                Prefix = prefix,
                Classes = classes
            };

            var homeroomTeacherIds = db.TeacherClassAssignments
                                       .Where(tca => tca.IsHeadTeacher == 1)
                                       .Select(tca => tca.TeacherId)
                                       .ToList();

            var availableTeachers = db.Teachers
                                      .Where(t => !homeroomTeacherIds.Contains(t.TeacherId))
                                      .Select(t => new TeacherViewModel
                                      {
                                          TeacherId = t.TeacherId,
                                          Name = t.Name
                                      })
                                      .ToList();

            ViewBag.Teachers = availableTeachers;

            return View(model);
        }

        [HttpPost]
        public ActionResult AddClass(AddClassViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var newClass = new Class
                    {
                        ClassName = model.ClassPrefix + model.ClassNameSuffix,
                        AcademicYear = model.AcademicYear
                    };

                    db.Classes.InsertOnSubmit(newClass);
                    db.SubmitChanges();
                    db.SubmitChanges();

                    return RedirectToAction("ClassesByPrefix1", new { year = model.AcademicYear, prefix = model.ClassPrefix });
                }
                else
                {
                    Debug.WriteLine("Model state is not valid.");
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            Debug.WriteLine($"Error in {state.Key}: {error.ErrorMessage}");
                        }
                    }

                    TempData["ErrorMessage"] = "Invalid data. Please check the input fields.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while adding the class. Please try again.";
            }

            // If model state is not valid or an error occurred, redirect back to the class list with an error
            return RedirectToAction("ClassesByPrefix1", new { year = model.AcademicYear, prefix = model.ClassPrefix });
        }









        public ActionResult ListTeachersBySubject(string subjectName)
        {
            var user = Session["khach"] as User;
            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            var teachers = db.Teachers
                             .Where(t => t.SubjectTC == subjectName)
                             .ToList();

            ViewBag.SubjectName = subjectName;
            return View(teachers);
        }
        public ActionResult ThongBaoDaoTao(int page = 1, int pageSize = 2)
        {
            var user = Session["khach"] as User;
            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            var notifications = db.Notifications
                                       .OrderByDescending(n => n.Timestamp)
                                       .Skip((page - 1) * pageSize)
                                       .Take(pageSize)
                                       .ToList();

            var totalNotifications = db.Notifications.Count();
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalNotifications / pageSize);
            ViewBag.CurrentPage = page;

            return View(notifications);
        }

        [HttpPost]
        public ActionResult AddNotification(string NameContent, string Content, int UserId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var notification = new Notification
                    {
                        NameContent = NameContent,
                        Content = Content,
                        Timestamp = DateTime.Now, // Set the current date and time
                        UserType = UserId
                    };

                    db.Notifications.InsertOnSubmit(notification);
                    db.SubmitChanges();

                    return RedirectToAction("ThongBaoDaoTao");
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Error saving notification: " + ex.Message;
                    return View("Error");
                }
            }

            return RedirectToAction("ThongBaoDaoTao");
        }

        [HttpPost]
        public ActionResult EditNotification(int id, string NameContent, string Content, int UserId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var notification = db.Notifications.FirstOrDefault(n => n.NotificationId == id);
                    if (notification != null)
                    {
                        notification.NameContent = NameContent;
                        notification.Content = Content;
                        notification.UserType = UserId;
                        db.SubmitChanges();
                    }
                    return RedirectToAction("ThongBaoDaoTao");
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Error editing notification: " + ex.Message;
                    return View("Error");
                }
            }

            return RedirectToAction("ThongBaoDaoTao");
        }

        [HttpPost]
        public ActionResult DeleteNotification(int id)
        {
            try
            {
                var notification = db.Notifications.FirstOrDefault(n => n.NotificationId == id);
                if (notification != null)
                {
                    db.Notifications.DeleteOnSubmit(notification);
                    db.SubmitChanges();
                }
                return RedirectToAction("ThongBaoDaoTao");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error deleting notification: " + ex.Message;
                return View("Error");
            }
        }
        public ActionResult AddTeacher1(string subjectName)
        {
            ViewBag.SubjectName = subjectName;
            var model = new Teacher();
            return View(model);
        }

        [HttpPost]
        public ActionResult AddTeacher1(Teacher teacher, string subjectName)
        {
            if (ModelState.IsValid)
            {
                // Check if the phone number already exists in the Users table
                var existingUser = db.Users.FirstOrDefault(u => u.Username == teacher.SDT);
                if (existingUser != null)
                {
                    ModelState.AddModelError("SDT", "Số điện thoại này đã tồn tại. Vui lòng nhập số điện thoại khác.");
                    ViewBag.SubjectName = subjectName;
                    return View(teacher);
                }

                // Create a new user account
                var user = new User
                {
                    Username = teacher.SDT,
                    Password = "e08013363fa73566cefd0bd6c6988ed7", // Pre-hashed password
                    UserType = 2
                };
                db.Users.InsertOnSubmit(user);
                db.SubmitChanges();

                // Get the UserId of the newly created account
                teacher.UserId = user.UserId;

                // Save the subject name to the teacher
                teacher.SubjectTC = subjectName;
                db.Teachers.InsertOnSubmit(teacher);
                db.SubmitChanges();

                return RedirectToAction("ListTeachersBySubject", new { subjectName = subjectName });
            }

            ViewBag.SubjectName = subjectName;
            return View(teacher);
        }




        public class TeacherViewModel
        {
            public int TeacherId { get; set; }
            public string Name { get; set; }
            public string SubjectTC { get; set; }
        }
        public ActionResult CreateTimetableForDay(int classId)
        {
            var currentYear = DateTime.Now.Year;

            var teachers = db.Teachers
                .Select(t => new TeacherViewModel
                {
                    TeacherId = t.TeacherId,
                    Name = t.Name,
                    SubjectTC = t.SubjectTC
                })
                .ToList();

            ViewBag.Teachers = teachers;
            ViewBag.SelectedClassId = classId;
            ViewBag.TimeSlots = new List<string>
        {
            "07:30 - 08:15", "08:15 - 09:00", "09:15 - 10:00", "10:00 - 10:45",
            "13:00 - 13:45", "13:45 - 14:30", "14:45 - 15:30", "15:30 - 16:15"
        };
            return View();
        }
        [HttpPost]
        public ActionResult CreateTimetableForDay(Timetable timetable)
        {
            if (ModelState.IsValid)
            {
                // Check for schedule conflicts within the same class
                var classConflict = db.Timetables.Any(t =>
                    t.ClassId == timetable.ClassId &&
                    t.Date == timetable.Date &&
                    t.Times == timetable.Times);

                if (classConflict)
                {
                    ModelState.AddModelError("", "Lớp học đã có lịch học vào thời gian này. Vui lòng chọn thời gian khác.");
                }
                else
                {
                    // Check for teacher conflicts
                    var teacherConflict = db.Timetables.Any(t =>
                        t.TeacherId == timetable.TeacherId &&
                        t.Date == timetable.Date &&
                        t.Times == timetable.Times);

                    if (teacherConflict)
                    {
                        ModelState.AddModelError("", "Giáo viên đã có lịch dạy vào thời gian này. Vui lòng chọn thời gian khác.");
                    }
                    else
                    {
                        db.Timetables.InsertOnSubmit(timetable);
                        db.SubmitChanges();
                        return RedirectToAction("ViewTimetablesByClass", new { classId = timetable.ClassId });
                    }
                }
            }

            var teachers = db.Teachers
                .Select(t => new TeacherViewModel
                {
                    TeacherId = t.TeacherId,
                    Name = t.Name,
                    SubjectTC = t.SubjectTC
                })
                .ToList();

            ViewBag.Teachers = teachers;
            ViewBag.TimeSlots = new List<string>
    {
        "07:30 - 08:15", "08:15 - 09:00", "09:15 - 10:00", "10:00 - 10:45",
        "13:00 - 13:45", "13:45 - 14:30", "14:45 - 15:30", "15:30 - 16:15"
    };
            return View(timetable);
        }


        public ActionResult Timetable()
        {
            var academicYears = db.Classes
                                  .Where(c => c.AcademicYear.HasValue)
                                  .Select(c => c.AcademicYear.Value)
                                  .Distinct()
                                  .ToList();
            return View(academicYears);
        }
        public ActionResult ViewClassesByYear(int academicYear, int page = 1, int pageSize = 6)
        {
            var classes = db.Classes
                            .Where(c => c.AcademicYear == academicYear)
                            .OrderBy(c => c.ClassName)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            int totalClasses = db.Classes.Count(c => c.AcademicYear == academicYear);
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalClasses / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.AcademicYear = academicYear;

            return View(classes);
        }
        public class TimetableViewModel2
        {
            public int TimetableId { get; set; }
            public int Weekdays { get; set; }
            public string Times { get; set; }
            public DateTime Date { get; set; }
            public string TeacherName { get; set; }
            public string SubjectName { get; set; }
        }


        public ActionResult ViewTimetablesByClass(int classId, int weekOffset = 0)
        {
            var user = Session["khach"] as User;
            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            DateTime startDate = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek + 1).AddDays(weekOffset * 7); // Get start of the week (Monday)
            DateTime endDate = startDate.AddDays(6); // Get end of the week (Sunday)

            var timetables = from t in db.Timetables
                             join teacher in db.Teachers on t.TeacherId equals teacher.TeacherId
                             where t.ClassId == classId && t.Date >= startDate && t.Date <= endDate
                             select new TimetableViewModel2
                             {
                                 TimetableId = t.TimetableId,
                                 Weekdays = t.Weekdays.Value,
                                 Times = t.Times,
                                 Date = t.Date.Value,
                                 TeacherName = teacher.Name,
                                 SubjectName = teacher.SubjectTC
                             };

            var timetableList = timetables.ToList();

            foreach (var item in timetableList)
            {
                Console.WriteLine($"Weekday: {item.Weekdays}, Time: {item.Times}, Date: {item.Date}, Teacher: {item.TeacherName}, Subject: {item.SubjectName}");
            }

            var classInfo = db.Classes.FirstOrDefault(c => c.ClassId == classId);

            ViewBag.ClassName = classInfo?.ClassName;
            ViewBag.ClassId = classId;
            ViewBag.WeekOffset = weekOffset;
            ViewBag.StartDate = startDate;
            ViewBag.AcademicYear = classInfo?.AcademicYear;

            return View(timetableList);
        }


        public ActionResult DeleteTimetable(int id)
        {
            var timetable = db.Timetables.FirstOrDefault(t => t.TimetableId == id);
            if (timetable != null)
            {
                db.Timetables.DeleteOnSubmit(timetable);
                db.SubmitChanges();
            }
            return RedirectToAction("ViewTimetablesByClass", new { classId = timetable.ClassId });
        }




        public ActionResult CreateTimetableForSemester(int classId)
        {
            var currentYear = DateTime.Now.Year;
            var classInfo = db.Classes.FirstOrDefault(c => c.ClassId == classId);

            if (classInfo == null || classInfo.AcademicYear != currentYear)
            {
                return RedirectToAction("ViewTimetablesByClass", new { classId = classId });
            }
            var teachers = db.Teachers
                .Select(t => new TeacherViewModel
                {
                    TeacherId = t.TeacherId,
                    Name = t.Name,
                    SubjectTC = t.SubjectTC
                })
                .ToList();

            ViewBag.Teachers = teachers;
            ViewBag.SelectedClassId = classId;
            ViewBag.TimeSlots = new List<string>
        {
            "07:30 - 08:15", "08:15 - 09:00", "09:15 - 10:00", "10:00 - 10:45",
            "13:00 - 13:45", "13:45 - 14:30", "14:45 - 15:30", "15:30 - 16:15"
        };
            return View();
        }
        [HttpPost]
        public ActionResult CreateTimetableForSemester(Timetable timetable)
        {
            if (ModelState.IsValid)
            {
                if (timetable.Date.HasValue)
                {
                    DateTime startDate = timetable.Date.Value;
                    bool conflictFound = false;

                    for (int i = 0; i < 35; i++)
                    {
                        DateTime currentStartDate = startDate.AddDays(i * 7);

                        // Check for conflicts
                        var conflict = db.Timetables.Any(t =>
                            t.TeacherId == timetable.TeacherId &&
                            t.Date == currentStartDate &&
                            t.Times == timetable.Times);

                        if (conflict)
                        {
                            conflictFound = true;
                            ModelState.AddModelError("", $"Giáo viên đã có lịch dạy vào ngày {currentStartDate:dd/MM/yyyy} vào giờ {timetable.Times}.");
                            break;
                        }
                    }

                    if (!conflictFound)
                    {
                        // Insert timetable entries for 35 weeks
                        for (int i = 0; i < 35; i++)
                        {
                            var newTimetable = new Timetable
                            {
                                ClassId = timetable.ClassId,
                                Date = startDate.AddDays(i * 7),
                                Times = timetable.Times,
                                TeacherId = timetable.TeacherId,
                                Weekdays = timetable.Weekdays
                            };
                            db.Timetables.InsertOnSubmit(newTimetable);
                        }

                        db.SubmitChanges();
                        return RedirectToAction("ViewTimetablesByClass", new { classId = timetable.ClassId });
                    }
                }
                else
                {
                    ModelState.AddModelError("Date", "Ngày là bắt buộc.");
                }
            }

            var teachers = db.Teachers
                .Select(t => new TeacherViewModel
                {
                    TeacherId = t.TeacherId,
                    Name = t.Name,
                    SubjectTC = t.SubjectTC
                })
                .ToList();

            ViewBag.Teachers = teachers;
            ViewBag.SelectedClassId = timetable.ClassId;
            ViewBag.TimeSlots = new List<string>
    {
        "07:30 - 08:15", "08:15 - 09:00", "09:15 - 10:00", "10:00 - 10:45",
        "13:00 - 13:45", "13:45 - 14:30", "14:45 - 15:30", "15:30 - 16:15"
    };

            return View(timetable);
        }


        public ActionResult ListTeacher()
        {
            List<Teacher> ds = tt.Teachers.ToList();
            return View(ds);
        }
        public ActionResult TeacherDetails(int teacherId)
        {

            return View();
        }
        public ActionResult AddTeacher()
        {

            return View();
        }
        [HttpPost]
        public ActionResult AddTeacher(Teacher tc)
        {
            tt.Teachers.InsertOnSubmit(tc);
            tt.SubmitChanges();
            return RedirectToAction("ListTeacher", "Home");
        }
        public ActionResult Editteacher(int id)
        {
            Teacher gv = tt.Teachers.FirstOrDefault(t => t.TeacherId == id);
            return View(gv);
        }
        [HttpPost]
        public ActionResult Editteacher(Teacher tc)
        {
            // Tìm kiếm giáo viên theo UserId
            Teacher gv = tt.Teachers.FirstOrDefault(t => t.TeacherId == tc.TeacherId);

            gv.Name = tc.Name;
            UpdateModel(gv);
            tt.SubmitChanges();
            return RedirectToAction("ListTeacher", "Home");
        }
        public ActionResult CreateAccount()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAccount([Bind(Include = "Name, SubjectTC, SDT, ...")] Teacher teacher)
        {
            if (ModelState.IsValid)
            {
                tt.Teachers.InsertOnSubmit(teacher);
                tt.SubmitChanges();
                return RedirectToAction("GiaoVien", "Home");
            }
            return View(teacher);
        }
        public ActionResult CreateScore(int studentId, int classId)
        {

            var user = Session["khach"] as User;
            if (user == null || user.UserType != 2)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            ViewBag.StudentId = studentId;
            ViewBag.ClassId = classId;

            return View();
        }
        [HttpPost]
        public ActionResult CreateScore(Score score)
        {
            if (ModelState.IsValid)
            {
                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    context.Scores.InsertOnSubmit(score);
                    context.SubmitChanges();
                }
                return RedirectToAction("ChiTietDiemGV", new { studentId = score.StudentId, classId = score.ClassId });
            }
            // Nếu dữ liệu không hợp lệ, hiển thị lại trang với thông báo lỗi
            return View(score);
        }
        public ActionResult EditScore(int id)
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var score = context.Scores.FirstOrDefault(s => s.ScoreId == id);
                if (score == null)
                {
                    return HttpNotFound();
                }
                return View(score);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditScore(Score score)
        {
            if (ModelState.IsValid)
            {
                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var existingScore = context.Scores.FirstOrDefault(s => s.ScoreId == score.ScoreId);
                    if (existingScore != null)
                    {
                        existingScore.Score15 = score.Score15;
                        existingScore.Score60 = score.Score60;
                        existingScore.GiuaKi = score.GiuaKi;
                        existingScore.CuoiKi = score.CuoiKi;
                        existingScore.TongKet = score.TongKet;
                        existingScore.SubjectTC = score.SubjectTC;
                        existingScore.Semester = score.Semester;
                        existingScore.Status = score.Status;
                        context.SubmitChanges();
                    }
                    return RedirectToAction("ChiTietDiemGV", new { studentId = score.StudentId, classId = score.ClassId });
                }
            }
            return View(score);
        }
        public ActionResult DeleteScore(int id)
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var score = context.Scores.FirstOrDefault(s => s.ScoreId == id);
                if (score == null)
                {
                    return HttpNotFound();
                }
                return View(score);
            }
        }
        [HttpPost, ActionName("DeleteScore")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteScoreConfirmed(int id)
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var score = context.Scores.FirstOrDefault(s => s.ScoreId == id);
                if (score != null)
                {
                    context.Scores.DeleteOnSubmit(score);
                    context.SubmitChanges();
                    return RedirectToAction("ChiTietDiemGV", new { studentId = score.StudentId, classId = score.ClassId });
                }
                return HttpNotFound();
            }
        }
        public ActionResult SuaDiem(int scoreId)
        {
            var user = Session["khach"] as User;
            if (user == null || user.UserType != 2)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var teacher = context.Teachers.FirstOrDefault(t => t.UserId == user.UserId);
                if (teacher == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                var score = context.Scores.FirstOrDefault(s => s.ScoreId == scoreId && s.TeacherId == teacher.TeacherId);
                if (score == null)
                {
                    return RedirectToAction("GiaoVien", "Home");
                }

                ViewBag.Score = score;
                ViewBag.StudentName = context.Students.FirstOrDefault(st => st.StudentId == score.StudentId)?.Name;
            }

            return View();
        }
        [HttpPost]
        public ActionResult SuaDiem(int scoreId, decimal score15, decimal score60, decimal giuaKi, decimal cuoiKi)
        {
            var user = Session["khach"] as User;
            if (user == null || user.UserType != 2)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var teacher = context.Teachers.FirstOrDefault(t => t.UserId == user.UserId);
                if (teacher == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                var score = context.Scores.FirstOrDefault(s => s.ScoreId == scoreId && s.TeacherId == teacher.TeacherId);
                if (score == null)
                {
                    return RedirectToAction("GiaoVien", "Home");
                }

                score.Score15 = score15;
                score.Score60 = score60;
                score.GiuaKi = giuaKi;
                score.CuoiKi = cuoiKi;
                score.TongKet = (score15 + score60 + giuaKi + cuoiKi) / 4;

                context.SubmitChanges();
            }

            return RedirectToAction("DiemTrongLop", "Home");// new { classId = score.ClassId });
        }

        public ActionResult ListParent()
        {
            List<Parent> ds = tt.Parents.ToList();
            return View(ds);
        }
        public ActionResult ParentDetails(int id)
        {
            Parent s = tt.Parents.FirstOrDefault(n => n.UserId == id);
            if (s == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            return View(s);
        }
        public ActionResult CreateAccountParent()
        {
            var user = new User();
            var parent = new Parent();
            var model = new Tuple<User, Parent>(user, parent);
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAccountParent([Bind(Prefix = "Item1")] User user, [Bind(Prefix = "Item2")] Parent parent)
        {
            if (ModelState.IsValid)
            {
                // Create new user
                user.UserType = 3; // Assuming UserType 3 is for parents

                tt.Users.InsertOnSubmit(user);
                tt.SubmitChanges();

                // Create new parent
                parent.UserId = user.UserId;

                tt.Parents.InsertOnSubmit(parent);
                tt.SubmitChanges();

                return RedirectToAction("PhuHuynh", "Home");
            }

            // Return the model in case of error
            var model = new Tuple<User, Parent>(user, parent);
            return View(model);
        }
        public ActionResult AddParent()
        {
            return View();
        }
        [HttpPost]
        public ActionResult AddParent(Parent p)
        {
            if (ModelState.IsValid)
            {
                tt.Parents.InsertOnSubmit(p);
                tt.SubmitChanges();
                return RedirectToAction("ListParent", "Home");
            }
            return View(p);
        }

        public ActionResult TrangChu()
        {
            if (Session["khach"] == null)
            {
                return RedirectToAction("Error", "Home");
            }
            return View();
        }
        public ActionResult ketQuaHocTap()
        {
            if (Session["khach"] == null)
            {
                return RedirectToAction("Error", "Home");
            }
            return View();
        }
        public ActionResult DaoTao()
        {
            if (Session["khach"] == null)
            {
                return RedirectToAction("Error", "Home");
            }
            return View();
        }

        public JsonResult GetTimetableByDate(string selectedDate)
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                DateTime date = DateTime.Parse(selectedDate);
                var timetableData = context.Timetables
                    .Where(t => t.Date == date)
                    .OrderBy(t => t.Times)
                    .ToList();

                return Json(timetableData, JsonRequestBehavior.AllowGet);
            }
        }

        public class GradeDaoTaoViewModel
        {
            public List<int?> Years { get; set; }
            public List<string> ClassPrefixes { get; set; }
            public int Year { get; set; }
        }

        public class ClassPrefixViewModel
        {
            public int Year { get; set; }
            public List<string> ClassPrefixes { get; set; }
        }

        public class ClassesByPrefixViewModel
        {
            public int Year { get; set; }
            public string Prefix { get; set; }
            public List<string> Classes { get; set; }
        }

        public ActionResult GradeDaoTao()
        {
            var years = db.Classes
                          .Select(c => c.AcademicYear)
                          .Distinct()
                          .OrderByDescending(y => y)
                          .ToList();

            var model = new GradeDaoTaoViewModel
            {
                Years = years
            };

            return View(model);
        }

        public ActionResult ClassPrefixes(int year)
        {
            var classPrefixes = db.Classes
                                  .Where(c => c.AcademicYear == year)
                                  .Select(c => c.ClassName.Substring(0, 2))
                                  .Distinct()
                                  .ToList();

            var model = new ClassPrefixViewModel
            {
                Year = year,
                ClassPrefixes = classPrefixes
            };

            return View(model);
        }

        public ActionResult ClassesByPrefix(int year, string prefix)
        {
            var classes = db.Classes
                            .Where(c => c.AcademicYear == year && c.ClassName.Substring(0, 2) == prefix)
                            .Select(c => c.ClassName)
                            .ToList();

            var model = new ClassesByPrefixViewModel
            {
                Year = year,
                Prefix = prefix,
                Classes = classes
            };

            return View(model);
        }
        public class StudentDetail
        {
            public int StudentId { get; set; }
            public string Name { get; set; }
            public string SDTPH { get; set; }
        }
        public class StudentsByClassViewModel
        {
            public int Year { get; set; }
            public string ClassName { get; set; }
            public List<StudentDetail> Students { get; set; }
        }


        public ActionResult StudentsByClass(int year, string className)
        {
            var students = db.Students
                             .Join(db.Classes, s => s.ClassId, c => c.ClassId, (s, c) => new { s, c })
                             .Where(sc => sc.c.AcademicYear == year && sc.c.ClassName == className)
                             .Select(sc => new StudentDetail
                             {
                                 StudentId = sc.s.StudentId,
                                 Name = sc.s.Name,
                                 SDTPH = sc.s.SDTPH
                             })
                             .ToList();

            var model = new StudentsByClassViewModel
            {
                Year = year,
                ClassName = className,
                Students = students
            };

            return View(model);
        }
        public class StudentDetailViewModel
        {
            public int StudentId { get; set; }
            public string Name { get; set; }
            public string SDTPH { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public string ClassName { get; set; }
            public string ParentName { get; set; } // Tên phụ huynh
            public string HomeroomTeacherName { get; set; } // Giáo viên chủ nhiệm
            public string SubjectName { get; set; }
            public string Email { get; set; } // Email
            public string Address { get; set; } // Địa chỉ
        }


        public ActionResult StudentDetail2(int studentId, int year, string className)
        {
            var student = (from s in db.Students
                           join c in db.Classes on s.ClassId equals c.ClassId
                           join psr in db.ParentStudentRelationships on s.StudentId equals psr.StudentId
                           join p in db.Parents on psr.ParentId equals p.ParentId
                           join tca in db.TeacherClassAssignments on c.ClassId equals tca.ClassId into tcaJoin
                           from tca in tcaJoin.DefaultIfEmpty()
                           join t in db.Teachers on tca.TeacherId equals t.TeacherId into tJoin
                           from t in tJoin.DefaultIfEmpty()
                           join st in db.SubjectTeachers on t.TeacherId equals st.TeacherId into stJoin
                           from st in stJoin.DefaultIfEmpty()
                           join sub in db.Subjects on st.SubjectId equals sub.SubjectId into subJoin
                           from sub in subJoin.DefaultIfEmpty()
                           where s.StudentId == studentId && c.AcademicYear == year && c.ClassName == className
                           select new StudentDetailViewModel
                           {
                               StudentId = s.StudentId,
                               Name = s.Name,
                               SDTPH = s.SDTPH,
                               DateOfBirth = s.DateOfBirth,
                               ClassName = c.ClassName,
                               ParentName = p.Name,
                               HomeroomTeacherName = t != null ? t.Name : "N/A", // Giáo viên chủ nhiệm có thể không có
                               SubjectName = sub != null ? sub.SubjectName : "N/A",
                               Email = p.Email, // Parent Email
                               Address = p.Address // Parent Address
                           }).FirstOrDefault();

            if (student == null)
            {
                return HttpNotFound();
            }

            ViewBag.Year = year;
            ViewBag.ClassName = className;
            return View(student);
        }





        public ActionResult DeleteStudent(int studentId, string className1, int year1)
        {
            int? classId;
            var student = db.Students.FirstOrDefault(s => s.StudentId == studentId);
            if (student == null)
            {
                return HttpNotFound();
            }
            classId = student.ClassId;
            var relatedScores = db.Scores.Where(s => s.StudentId == studentId).ToList();
            if (relatedScores.Any())
            {
                db.Scores.DeleteAllOnSubmit(relatedScores);
            }
            var relatedRelationships = db.ParentStudentRelationships.Where(ps => ps.StudentId == studentId).ToList();
            if (relatedRelationships.Any())
            {
                db.ParentStudentRelationships.DeleteAllOnSubmit(relatedRelationships);
            }
            db.Students.DeleteOnSubmit(student);
            db.SubmitChanges();

            // Redirect to the class list page after deletion
            return RedirectToAction("StudentsByClass", new { year = year1, className = className1 });
        }
        public class GradeViewModel
        {
            public int StudentId { get; set; }
            public string StudentName { get; set; }
            public List<SubjectGrade> SubjectGrades { get; set; }
            public int Semester { get; set; }
            public int Year { get; set; }
        }

        public class SubjectGrade
        {
            public string SubjectName { get; set; }
            public string Scores { get; set; }
            public decimal? Score15 { get; set; }
            public decimal? Score60 { get; set; }
            public decimal? GiuaKi { get; set; }
            public decimal? CuoiKi { get; set; }

            public decimal? TongKet { get; set; }

            public int? Semester  {get;set;}

            public int? AcademicYear { get; set; }

            public string Status { get; set; }
        }



        public ActionResult ViewGrades(int studentId)
        {
            var grades = db.Scores
                           .Where(s => s.StudentId == studentId)
                           .Select(s => new SubjectGrade
                           {
                               SubjectName = s.SubjectTC,
                               Scores = s.Scores,
                               Score15 = s.Score15,
                               Score60 = s.Score60,
                               GiuaKi = s.GiuaKi,
                               CuoiKi = s.CuoiKi,
                               TongKet = s.TongKet,
                               Semester = s.Semester,
                               AcademicYear = s.Class.AcademicYear,
                               Status = s.Status
                           })
                           .ToList();

            var studentName = db.Students
                                .Where(s => s.StudentId == studentId)
                                .Select(s => s.Name)
                                .FirstOrDefault();

            var model = new GradeViewModel
            {
                StudentId = studentId,
                StudentName = studentName,
                SubjectGrades = grades
            };

            return View(model);
        }













        public class AddStudentViewModel
        {
            public string Name { get; set; }
            public string SDTPH { get; set; }
            public DateTime DateOfBirth { get; set; }
            public int Year { get; set; }
            public string ClassName { get; set; }
            public int? ClassId { get; set; }
            public int GradeId { get; set; } // Existing property

            // New properties for parent information
            public string ParentName { get; set; }
            public string ParentEmail { get; set; }
            public string ParentAddress { get; set; }
        }


        public JsonResult IsPhoneNumberAvailable(string phoneNumber)
        {
            var existingUser = db.Users.FirstOrDefault(u => u.Username == phoneNumber);
            if (existingUser != null)
            {
                var parentInfo = db.Parents.FirstOrDefault(p => p.UserId == existingUser.UserId);
                return Json(new
                {
                    isAvailable = false,
                    parentName = parentInfo?.Name,
                    parentEmail = parentInfo?.Email,
                    parentAddress = parentInfo?.Address
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { isAvailable = true }, JsonRequestBehavior.AllowGet);
        }




        public ActionResult AddStudent1(int year, string className)
        {
            ViewBag.Year = year;
            ViewBag.ClassName = className;

            var model = new AddStudentViewModel
            {
                Year = year,
                ClassName = className
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddStudent1(AddStudentViewModel model, string ParentConfirmation)
        {
            if (ModelState.IsValid)
            {
                var existingUser = db.Users.FirstOrDefault(u => u.Username == model.SDTPH);
                int userId;
                int parentId;

                if (existingUser != null)
                {
                    if (ParentConfirmation == null)
                    {
                        // Prompt for confirmation if not provided
                        ViewBag.ShowConfirmation = true;
                        ViewBag.ParentName = existingUser.Username; // Assuming Username is the parent's name
                        ViewBag.Year = model.Year;
                        ViewBag.ClassName = model.ClassName;
                        return View(model);
                    }

                    if (ParentConfirmation == "yes")
                    {
                        userId = existingUser.UserId;
                        parentId = db.Parents.FirstOrDefault(p => p.UserId == userId).ParentId;
                    }
                    else
                    {
                        ModelState.AddModelError("", "User with this SDTPH already exists.");
                        ViewBag.Year = model.Year;
                        ViewBag.ClassName = model.ClassName;
                        return View(model);
                    }
                }
                else
                {
                    // Create new user
                    var user = new User
                    {
                        Username = model.SDTPH,
                        Password = "e08013363fa73566cefd0bd6c6988ed7", // MD5 hash
                        UserType = 1
                    };

                    db.Users.InsertOnSubmit(user);
                    db.SubmitChanges();

                    userId = user.UserId;

                    // Create new parent
                    var parent = new Parent
                    {
                        UserId = userId,
                        Name = model.ParentName,
                        Email = model.ParentEmail,
                        Address = model.ParentAddress
                    };

                    db.Parents.InsertOnSubmit(parent);
                    db.SubmitChanges();

                    parentId = parent.ParentId;
                }

                var classInfo = db.Classes
                                .Where(c => c.AcademicYear == model.Year && c.ClassName == model.ClassName)
                                .Select(c => new { c.ClassId, c.GradeId })
                                .FirstOrDefault();

                if (classInfo != null)
                {
                    var student = new Student
                    {
                        Name = model.Name,
                        SDTPH = model.SDTPH,
                        DateOfBirth = model.DateOfBirth,
                        ClassId = classInfo.ClassId,
                        UserId = userId,
                        GradeID = classInfo.GradeId // Save the GradeId
                    };

                    db.Students.InsertOnSubmit(student);
                    db.SubmitChanges();

                    // Establish relationship between parent and student
                    var relationship = new ParentStudentRelationship
                    {
                        ParentId = parentId,
                        StudentId = student.StudentId
                    };

                    db.ParentStudentRelationships.InsertOnSubmit(relationship);
                    db.SubmitChanges();

                    return RedirectToAction("StudentsByClass", new { year = model.Year, className = model.ClassName });
                }
                else
                {
                    ModelState.AddModelError("", "ClassId not found.");
                }
            }

            ViewBag.Year = model.Year;
            ViewBag.ClassName = model.ClassName;

            return View(model);
        }




        public JsonResult CheckParent(string sdtph)
        {
            var user = db.Users.FirstOrDefault(u => u.Username == sdtph);
            return Json(new { exists = user != null }, JsonRequestBehavior.AllowGet);
        }



        public ActionResult DetailClass(int classId, int? year)
        {
            List<Student> students;
            int? nextClassId;
            List<Class> classes;
            Class currentClass;

            using (var context = new db_sll2DataContext(@"Data Source=Fate;Initial Catalog=soLienLac;Integrated Security=True"))
            {
                students = context.Students
                                  .Where(s => s.ClassId == classId && (!year.HasValue || s.Class.AcademicYear == year))
                                  .ToList();

                nextClassId = context.Classes
                                     .Where(c => c.ClassId > classId && (!year.HasValue || c.AcademicYear == year))
                                     .OrderBy(c => c.ClassId)
                                     .Select(c => (int?)c.ClassId)
                                     .FirstOrDefault();

                // Retrieve all classes
                classes = context.Classes.ToList();

                // Retrieve the current class
                currentClass = context.Classes.FirstOrDefault(c => c.ClassId == classId && (!year.HasValue || c.AcademicYear == year));
            }

            if (students == null || students.Count == 0)
            {
                // Handle case where no students are found for the given classId and year
                return RedirectToAction("Index", "Home"); // Or some other action
            }

            ViewBag.ClassId = classId;
            ViewBag.Year = year ?? currentClass?.AcademicYear;
            ViewBag.NextClassId = nextClassId;
            ViewBag.Classes = classes; // Pass all classes to the view
            ViewBag.CurrentClassName = currentClass?.ClassName;
            return View(students);
        }

        public ActionResult ClassesByAcademicYear(int academicYear, int lop)
        {
            if (Session["khach"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classes = (from c in context.Classes
                               join g in context.Grades on c.GradeId equals g.GradeID
                               where c.AcademicYear == academicYear && g.Grade1 == lop
                               select c.ClassName)
                              .ToList();
                ViewBag.AcademicYear = academicYear;
                ViewBag.Grade = lop;
                return View(classes);
            }
        }
        public ActionResult StudentsInClass(int academicYear, string className)
        {
            if (Session["khach"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var students = (from s in context.Students
                                join c in context.Classes on s.ClassId equals c.ClassId
                                join g in context.Grades on c.GradeId equals g.GradeID
                                where c.AcademicYear == academicYear && c.ClassName == className
                                select s)
                               .ToList();
                ViewBag.AcademicYear = academicYear;
                ViewBag.ClassName = className;
                return View(students);
            }
        }
        public ActionResult StudentDetails(int studentId)
        {
            if (Session["khach"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var student = context.Students.FirstOrDefault(s => s.StudentId == studentId);

                if (student == null)
                {
                    // Xử lý khi không tìm thấy sinh viên
                    return RedirectToAction("StudentNotFound");
                }
                var className = context.Classes.FirstOrDefault(c => c.ClassId == student.ClassId)?.ClassName;
                ViewBag.ClassName = className;
                return View(student);
            }
        }

        public ActionResult PhepNghi()
        {
            if (Session["khach"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            return View();
        }
        public class LeaveRequestViewModel1
        {
            public int RequestId { get; set; }
            public string TeacherName { get; set; }
            public string SubjectTC { get; set; }
            public string Reason { get; set; }
            public DateTime? RequestDate { get; set; }
            public string ApprovalStatus { get; set; }
        }


        public ActionResult PhepNghiGiaoVien(int page = 1, int pageSize = 7)
        {
            if (Session["khach"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            var leaveRequests = (from lr in db.LeaveRequests
                                 join t in db.Teachers on lr.TeacherId equals t.TeacherId
                                 where lr.StudentId == null
                                 select new LeaveRequestViewModel1
                                 {
                                     RequestId = lr.RequestId,
                                     TeacherName = t.Name,
                                     SubjectTC = t.SubjectTC,
                                     Reason = lr.Reason,
                                     RequestDate = lr.RequestDate,
                                     ApprovalStatus = lr.ApprovalStatus
                                 }).ToList();

            var paginatedRequests = leaveRequests.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.TotalPages = (int)Math.Ceiling((double)leaveRequests.Count / pageSize);
            ViewBag.CurrentPage = page;

            return View(paginatedRequests);
        }
        [HttpPost]
        public ActionResult UpdateApprovalStatus1(int requestId, string status)
        {
            try
            {
                var request = db.LeaveRequests.FirstOrDefault(lr => lr.RequestId == requestId);
                if (request != null)
                {
                    request.ApprovalStatus = status;
                    request.RequestDate = DateTime.Now;
                    db.SubmitChanges();
                    TempData["Message"] = "Cập nhật trạng thái thành công!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy yêu cầu!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("PhepNghiGiaoVien");
        }

        public class LeaveRequestViewModel2
        {
            public int RequestId { get; set; }
            public string StudentName { get; set; }
            public string ClassName { get; set; }
            public string Reason { get; set; }
            public DateTime? RequestDate { get; set; }
            public string ApprovalStatus { get; set; }
            public int? AcademicYear { get; set; }
        }

        public ActionResult PhepNghiHocSinh(int page = 1, int pageSize = 7)
        {
            if (Session["khach"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            var leaveRequests = (from lr in db.LeaveRequests
                                 join s in db.Students on lr.StudentId equals s.StudentId
                                 join c in db.Classes on s.ClassId equals c.ClassId
                                 where lr.StudentId != null
                                 select new LeaveRequestViewModel2
                                 {
                                     RequestId = lr.RequestId,
                                     StudentName = s.Name,
                                     ClassName = c.ClassName,
                                     Reason = lr.Reason,
                                     RequestDate = lr.RequestDate,
                                     ApprovalStatus = lr.ApprovalStatus,
                                     AcademicYear = c.AcademicYear
                                 }).ToList();
            var paginatedRequests = leaveRequests.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.TotalPages = (int)Math.Ceiling((double)leaveRequests.Count / pageSize);
            ViewBag.CurrentPage = page;

            return View(leaveRequests);
        }

        [HttpPost]
        public ActionResult UpdateApprovalStatus2(int requestId, string status)
        {
            try
            {
                var leaveRequest = db.LeaveRequests.FirstOrDefault(lr => lr.RequestId == requestId);
                if (leaveRequest != null)
                {
                    leaveRequest.ApprovalStatus = status;
                    db.SubmitChanges();
                    TempData["Message"] = "Cập nhật trạng thái thành công!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy yêu cầu!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }
            return RedirectToAction("PhepNghiHocSinh");
        }














        //Hau ne bro

        public static string GetMD5(string str)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] fromData = Encoding.UTF8.GetBytes(str);
            byte[] targetData = md5.ComputeHash(fromData);
            string byte2String = null;

            for (int i = 0; i < targetData.Length; i++)
            {
                byte2String += targetData[i].ToString("x2");
            }
            return byte2String;
        }
        public ActionResult DangXuat()
        {
            Session.Clear();
            return RedirectToAction("DangNhap");
        }
        public ActionResult DangNhap(string tenDangNhap, string matKhau)
        {
            if (string.IsNullOrEmpty(tenDangNhap) || string.IsNullOrEmpty(matKhau))
            {
                var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True");
                var notifications = context.Notifications.ToList();
                return View(notifications);
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                string hashedPassword = GetMD5(matKhau);

                var user = context.Users.FirstOrDefault(u => u.Username == tenDangNhap && u.Password == hashedPassword);

                if (user != null)
                {
                    Session["khach"] = user;

                    if (user.UserType == 0)
                    {
                        return RedirectToAction("DaoTao", "Home");
                    }
                    else if (user.UserType == 1)
                    {
                        return RedirectToAction("PhuHuynh", "Home");
                    }
                    else if (user.UserType == 2)
                    {
                        return RedirectToAction("GiaoVien", "Home");
                    }
                }
                else
                {
                    ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng. Vui lòng thử lại.";
                }

                var notifications = context.Notifications.ToList();
                return View(notifications);
            }
        }
        public ActionResult Login()
        {
            return View();
        }
        public string IsPasswordValid(string password)
        {
            if (password.Length < 8)
                return "Mật khẩu cần có ít nhất 8 ký tự.";

            if (!password.Any(char.IsUpper))
                return "Mật khẩu cần chứa ít nhất một chữ hoa (A-Z).";

            if (!password.Any(char.IsLower))
                return "Mật khẩu cần chứa ít nhất một chữ thường (a-z).";

            if (!password.Any(char.IsDigit))
                return "Mật khẩu cần chứa ít nhất một số (0-9).";

            string specialChars = @"%!@#$%^&*()?/>.<,:;'\|}]{[_~`+=-" + "\"";
            if (!password.Any(specialChars.Contains))
                return "Mật khẩu cần chứa ít nhất một ký tự đặc biệt (ví dụ: !#$%^&*).";

            return null; // Return null if all checks pass, indicating no errors.
        }

        public ActionResult QuenMatKhau()
        {
            return View();
        }
        [HttpPost]
        public ActionResult QuenMatKhau(string phoneNumber)
        {
            try
            {
                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var student = context.Students.FirstOrDefault(s => s.SDTPH == phoneNumber);
                    if (student == null)
                    {
                        ViewBag.ErrorMessage = "Không có số điện thoại này trong hệ thống.";
                        return View();
                    }

                    var parent = context.Parents.FirstOrDefault(p => p.UserId == student.UserId);
                    if (parent == null)
                    {
                        ViewBag.ErrorMessage = "Không tìm thấy phụ huynh tương ứng.";
                        return View();
                    }

                    var user = context.Users.FirstOrDefault(u => u.UserId == parent.UserId);
                    if (user == null)
                    {
                        ViewBag.ErrorMessage = "Không tìm thấy tài khoản người dùng.";
                        return View();
                    }

                    ViewBag.StudentInfo = student;
                    ViewBag.UserId = user.UserId;

                    var className = context.Classes.FirstOrDefault(c => c.ClassId == student.ClassId)?.ClassName;
                    ViewBag.ClassName = className;

                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại.";
                return View();
            }
        }
        [HttpPost]
        public ActionResult DatLaiMatKhau(int? userId, string newPassword, string confirmPassword)
        {
            if (userId == null)
            {
                ViewBag.ErrorMessage = "Không tìm thấy người dùng để đặt lại mật khẩu.";
                return View("QuenMatKhau");
            }
            if (newPassword != confirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.";
                ViewBag.UserId = userId;
                return View("QuenMatKhau");
            }

            string passwordValidationResult = IsPasswordValid(newPassword);
            if (passwordValidationResult != null)
            {
                ViewBag.ErrorMessage = passwordValidationResult;
                ViewBag.UserId = userId;
                return View("QuenMatKhau");
            }

            try
            {
                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var user = context.Users.FirstOrDefault(u => u.UserId == userId);
                    if (user == null)
                    {
                        ViewBag.ErrorMessage = "Không tìm thấy tài khoản người dùng.";
                        return View("QuenMatKhau");
                    }

                    // Assume GetMD5 is a method to hash the password
                    user.Password = GetMD5(newPassword);
                    context.SubmitChanges();

                    return RedirectToAction("DangNhap", "Home");
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra trong quá trình đặt lại mật khẩu. Vui lòng thử lại.";
                ViewBag.UserId = userId;
                return View("QuenMatKhau");
            }
        }

        public ActionResult GiaoVien()
        {
            var user = Session["khach"] as User;
            if (user == null || user.UserType != 2)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var teacher = context.Teachers.FirstOrDefault(t => t.UserId == user.UserId);
                if (teacher == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                var currentYear = DateTime.Now.Year;

                var pastClasses = new Dictionary<Class, string>();
                var currentClasses = new Dictionary<Class, string>();

                var teachingClasses = context.TeacherClassAssignments
                    .Where(tca => tca.TeacherId == teacher.TeacherId)
                    .Select(tca => new { tca.Class, tca.AcademicYear, tca.IsHeadTeacher })
                    .ToList();

                foreach (var tc in teachingClasses)
                {
                    var classDetails = $"Lớp: {tc.Class.ClassName}, Năm: {tc.AcademicYear}";
                    if (tc.AcademicYear < currentYear)
                    {
                        classDetails += tc.IsHeadTeacher == 1 ? " - đã chủ nhiệm" : " - đã dạy";
                        pastClasses[tc.Class] = classDetails;
                    }
                    else
                    {
                        classDetails += tc.IsHeadTeacher == 1 ? " - đang chủ nhiệm" : " - đang dạy";
                        currentClasses[tc.Class] = classDetails;
                    }
                }

                ViewBag.TeacherName = teacher.Name;
                ViewBag.Subject = teacher.SubjectTC;
                ViewBag.PastClasses = pastClasses;
                ViewBag.CurrentClasses = currentClasses;
                ViewBag.TeacherId = teacher.TeacherId;
            }

            return View();
        }



        public ActionResult XemThoiKhoaBieuGiaoVien(int teacherId, DateTime? date, int weekOffset = 0)
        {
            try
            {
                var user = Session["khach"] as User;
                if (user == null || user.UserType != 2)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);
                    if (teacher == null)
                    {
                        return HttpNotFound();
                    }

                    if (!date.HasValue)
                    {
                        date = DateTime.Now;
                    }

                    date = date.Value.AddDays(weekOffset * 7);
                    var startDate = date.Value.AddDays(-(int)date.Value.DayOfWeek + (int)DayOfWeek.Monday);

                    IQueryable<Timetable> timetableQuery = context.Timetables
                        .Where(t => t.TeacherId == teacherId && t.Date >= startDate && t.Date < startDate.AddDays(7));

                    var timetable = timetableQuery
                        .OrderBy(t => t.Weekdays)
                        .ThenBy(t => t.Times)
                        .Select(t => new TimetableDetailViewModel
                        {
                            Weekday = t.Weekdays ?? default(int),
                            Time = t.Times,
                            SubjectName = context.SubjectTeachers
                                                .Where(st => st.TeacherId == t.TeacherId)
                                                .Join(context.Subjects,
                                                      st => st.SubjectId,
                                                      s => s.SubjectId,
                                                      (st, s) => s.SubjectName)
                                                .FirstOrDefault(),
                            ClassName = context.Classes
                                               .Where(c => c.ClassId == t.ClassId)
                                               .Select(c => c.ClassName)
                                               .FirstOrDefault()
                        })
                        .ToList();

                    var viewModel = new TeacherTimetableViewModel
                    {
                        Teacher = teacher,
                        TimetableDetails = timetable,
                        StartDate = startDate
                    };

                    ViewData["CurrentDate"] = date;

                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                // Log exception or handle it as needed
                throw;
            }
        }
        public ActionResult ChangePassword()
        {
            var user = Session["khach"] as User;
            if (user == null || user.UserType != 2)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            return View();
        }
        [HttpPost]
        public ActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var user = Session["khach"] as User;
            if (user == null || user.UserType != 2)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            if (NewPassword != ConfirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp.";
                return View();
            }

            string passwordValidationError = IsPasswordValid(NewPassword);
            if (passwordValidationError != null)
            {
                ViewBag.ErrorMessage = passwordValidationError;
                return View();
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var dbUser = context.Users.FirstOrDefault(u => u.UserId == user.UserId);
                if (dbUser == null || dbUser.Password != GetMD5(CurrentPassword))
                {
                    ViewBag.ErrorMessage = "Mật khẩu hiện tại không đúng.";
                    return View();
                }

                dbUser.Password = GetMD5(NewPassword);
                context.SubmitChanges();
            }

            ViewBag.SuccessMessage = "Đổi mật khẩu thành công.";
            return View();
        }
        public ActionResult ChangePasswordPH()
        {
            var user = Session["khach"] as User;
            if (user == null || user.UserType != 1)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            return View();
        }
        [HttpPost]
        public ActionResult ChangePasswordPH(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var user = Session["khach"] as User;
            if (user == null || user.UserType != 1)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            if (NewPassword != ConfirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp.";
                return View();
            }

            string passwordValidationError = IsPasswordValid(NewPassword);
            if (passwordValidationError != null)
            {
                ViewBag.ErrorMessage = passwordValidationError;
                return View();
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var dbUser = context.Users.FirstOrDefault(u => u.UserId == user.UserId);
                if (dbUser == null || dbUser.Password != GetMD5(CurrentPassword))
                {
                    ViewBag.ErrorMessage = "Mật khẩu hiện tại không đúng.";
                    return View();
                }

                dbUser.Password = GetMD5(NewPassword);
                context.SubmitChanges();
            }

            ViewBag.SuccessMessage = "Đổi mật khẩu thành công.";
            return View();
        }
        public ActionResult ExportClassScoresToExcel(int classId, int teacherId, int semester)
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var students = context.Students
                                      .Where(s => s.ClassId == classId)
                                      .Select(s => new StudentScoreViewModel
                                      {
                                          StudentId = s.StudentId,
                                          Name = s.Name,
                                          Scores = context.Scores.Where(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester).Select(sc => sc.Scores).FirstOrDefault(),
                                          Score15 = context.Scores.Where(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester).Select(sc => sc.Score15).FirstOrDefault(),
                                          Score60 = context.Scores.Where(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester).Select(sc => sc.Score60).FirstOrDefault(),
                                          GiuaKi = context.Scores.Where(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester).Select(sc => sc.GiuaKi).FirstOrDefault(),
                                          CuoiKi = context.Scores.Where(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester).Select(sc => sc.CuoiKi).FirstOrDefault(),
                                          Semester = semester,
                                      }).ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("DanhSachLop");
                    var row = 1;

                    worksheet.Cells[row, 1].Value = "Tên Sinh Viên";
                    worksheet.Cells[row, 2].Value = "Điểm Thường xuyên";
                    worksheet.Cells[row, 3].Value = "Điểm 15 Phút";
                    worksheet.Cells[row, 4].Value = "Điểm 60 Phút";
                    worksheet.Cells[row, 5].Value = "Điểm Giữa Kỳ";
                    worksheet.Cells[row, 6].Value = "Điểm Cuối Kỳ";
                    worksheet.Cells[row, 7].Value = "Học Kỳ";

                    foreach (var student in students)
                    {
                        row++;
                        worksheet.Cells[row, 1].Value = student.Name;
                        worksheet.Cells[row, 2].Value = student.Scores;
                        worksheet.Cells[row, 3].Value = student.Score15;
                        worksheet.Cells[row, 4].Value = student.Score60;
                        worksheet.Cells[row, 5].Value = student.GiuaKi;
                        worksheet.Cells[row, 6].Value = student.CuoiKi;
                        worksheet.Cells[row, 7].Value = student.Semester;
                    }

                    // Lưu file Excel
                    var excelData = package.GetAsByteArray();
                    var fileName = $"DanhSachLop_{classId}_Semester_{semester}.xlsx";
                    return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
        public ActionResult ExportStudentScoresToExcel(int studentId, int? semester, int? academicYear)
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var scoresQuery = context.Scores.Where(s => s.StudentId == studentId);

                if (semester.HasValue)
                {
                    scoresQuery = scoresQuery.Where(s => s.Semester == semester.Value);
                }

                if (academicYear.HasValue)
                {
                    var gradeIds = context.Grades.Where(g => g.AcademicYear == academicYear.Value).Select(g => g.GradeID).ToList();
                    scoresQuery = scoresQuery.Where(s => gradeIds.Contains((int)s.GradeID));
                }

                var scores = scoresQuery.ToList();

                if (!scores.Any())
                {
                    return HttpNotFound();
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("ChiTietDiem");
                    var row = 1;

                    worksheet.Cells[row, 1].Value = "Môn Học";
                    worksheet.Cells[row, 2].Value = "Thường xuyên";
                    worksheet.Cells[row, 3].Value = "Điểm 15'";
                    worksheet.Cells[row, 4].Value = "Điểm 60'";
                    worksheet.Cells[row, 5].Value = "Giữa Kỳ";
                    worksheet.Cells[row, 6].Value = "Cuối Kỳ";
                    worksheet.Cells[row, 7].Value = "Tổng Kết";
                    worksheet.Cells[row, 8].Value = "Kỳ";
                    worksheet.Cells[row, 9].Value = "Trạng Thái";

                    foreach (var score in scores)
                    {
                        row++;
                        worksheet.Cells[row, 1].Value = score.SubjectTC;
                        worksheet.Cells[row, 2].Value = score.Scores;
                        worksheet.Cells[row, 3].Value = score.Score15;
                        worksheet.Cells[row, 4].Value = score.Score60;
                        worksheet.Cells[row, 5].Value = score.GiuaKi;
                        worksheet.Cells[row, 6].Value = score.CuoiKi;
                        worksheet.Cells[row, 7].Value = score.TongKet;
                        worksheet.Cells[row, 8].Value = score.Semester;
                        worksheet.Cells[row, 9].Value = score.Status;
                    }

                    var excelData = package.GetAsByteArray();
                    var fileName = $"ChiTietDiem_{studentId}.xlsx";
                    return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
        public ActionResult UploadExcel(HttpPostedFileBase excelFile, int classId, int teacherId, int semester)
        {
            if (excelFile == null || excelFile.ContentLength <= 0)
            {
                ModelState.AddModelError("", "Vui lòng tải lên tệp Excel hợp lệ.");
                return RedirectToAction("DanhSachLop", new { classId, teacherId, semester });
            }

            string directoryPath = Server.MapPath("~/App_Data/Uploads");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string uniqueFileName = Path.GetFileNameWithoutExtension(excelFile.FileName) + "_" + Guid.NewGuid().ToString() + Path.GetExtension(excelFile.FileName);
            string filePath = Path.Combine(directoryPath, uniqueFileName);
            excelFile.SaveAs(filePath);

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    ModelState.AddModelError("", "Tệp Excel không hợp lệ.");
                    return RedirectToAction("DanhSachLop", new { classId, teacherId, semester });
                }

                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                    var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);
                    if (classInfo == null || teacher == null)
                    {
                        ModelState.AddModelError("", "Class or Teacher not found.");
                        return RedirectToAction("DanhSachLop", new { classId, teacherId, semester });
                    }

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        if (worksheet.Cells[row, 1].Value == null)
                        {
                            continue;
                        }

                        string studentName = worksheet.Cells[row, 1].Value.ToString();
                        var students = context.Students.Where(s => s.Name.Equals(studentName) && s.ClassId == classId).ToList();

                        if (students.Count > 1)
                        {
                            ModelState.AddModelError("", "Có nhiều hơn một học sinh trùng tên: " + studentName + " trong lớp.");
                            continue;
                        }
                        else if (students.Count == 0)
                        {
                            ModelState.AddModelError("", "Không tìm thấy học sinh: " + studentName + " trong lớp.");
                            continue;
                        }

                        var student = students.First();
                        int studentId = student.StudentId;

                        var scores = context.Scores.FirstOrDefault(s => s.StudentId == studentId && s.TeacherId == teacherId && s.Semester == semester);
                        if (scores == null)
                        {
                            scores = new Score
                            {
                                StudentId = studentId,
                                TeacherId = teacherId,
                                Semester = semester,
                                ClassId = classId,
                                GradeID = classInfo.GradeId,
                                SubjectTC = teacher.SubjectTC
                            };
                            context.Scores.InsertOnSubmit(scores);
                        }
                        else
                        {
                            scores.ClassId = classId;
                            scores.GradeID = classInfo.GradeId;
                            scores.SubjectTC = teacher.SubjectTC;
                        }

                        scores.Scores = worksheet.Cells[row, 2].Value?.ToString();
                        scores.Score15 = worksheet.Cells[row, 3].Value != null ? (decimal?)Convert.ToDouble(worksheet.Cells[row, 3].Value) : null;
                        scores.Score60 = worksheet.Cells[row, 4].Value != null ? (decimal?)Convert.ToDouble(worksheet.Cells[row, 4].Value) : null;
                        scores.GiuaKi = worksheet.Cells[row, 5].Value != null ? (decimal?)Convert.ToDouble(worksheet.Cells[row, 5].Value) : null;
                        scores.CuoiKi = worksheet.Cells[row, 6].Value != null ? (decimal?)Convert.ToDouble(worksheet.Cells[row, 6].Value) : null;

                        CalculateFinalScoreAndStatus(scores, semester, context, teacher.SubjectTC);
                    }
                    context.SubmitChanges();
                }
            }

            return RedirectToAction("DanhSachLop", new { classId, teacherId, semester });
        }

        [HttpPost]
        public ActionResult UpdateScores(int classId, int teacherId, int semester, List<StudentScoreViewModel> students)
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);
                if (classInfo == null || teacher == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Class or Teacher not found");
                }

                foreach (var student in students)
                {
                    var score = context.Scores.FirstOrDefault(s => s.ScoreId == student.ScoreId);
                    if (score != null)
                    {
                        score.Scores = student.Scores;
                        score.Score15 = student.Score15;
                        score.Score60 = student.Score60;
                        score.GiuaKi = student.GiuaKi;
                        score.CuoiKi = student.CuoiKi;

                        score.ClassId = classId;
                        score.GradeID = classInfo.GradeId;
                        score.SubjectTC = teacher.SubjectTC;
                    }
                }
                context.SubmitChanges();
            }

            return RedirectToAction("DanhSachLop", new { classId, teacherId, semester });
        }

        public ActionResult DanhSachLop(int classId, int teacherId, int semester = 1, bool isCurrentClass = true)
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                if (classInfo == null)
                {
                    return HttpNotFound("Class not found.");
                }

                var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);
                if (teacher == null)
                {
                    return HttpNotFound("Teacher not found.");
                }

                var students = context.Students
                    .Where(s => s.ClassId == classId)
                    .Select(s => new
                    {
                        Student = s,
                        Scores = context.Scores
                            .Where(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester)
                            .FirstOrDefault()
                    })
                    .ToList();

                var studentScores = students
                    .Select(s => new StudentScoreViewModel
                    {
                        StudentId = s.Student.StudentId,
                        Name = s.Student.Name,
                        Scores = s.Scores?.Scores,
                        Score15 = s.Scores?.Score15,
                        Score60 = s.Scores?.Score60,
                        GiuaKi = s.Scores?.GiuaKi,
                        CuoiKi = s.Scores?.CuoiKi,
                        TongKet = s.Scores?.TongKet,
                        Status = s.Scores?.Status,
                        Semester = semester,
                    })
                    .ToList();

                var semesters = new List<int> { 1, 2 };
                var existingSemesters = context.Scores
                    .Where(sc => sc.Student.ClassId == classId && sc.TeacherId == teacherId)
                    .Select(sc => sc.Semester)
                    .Distinct()
                    .ToList();

                foreach (var sem in semesters)
                {
                    if (!existingSemesters.Contains(sem))
                    {
                        foreach (var student in students)
                        {
                            var defaultScore = new Score
                            {
                                StudentId = student.Student.StudentId,
                                TeacherId = teacherId,
                                Semester = sem,
                                Scores = "",
                                Score15 = 0,
                                Score60 = 0,
                                GiuaKi = 0,
                                CuoiKi = 0,
                                TongKet = 0,
                                Status = "N/A",
                                ClassId = classId,
                                GradeID = classInfo.GradeId,
                                SubjectTC = teacher.SubjectTC
                            };
                            context.Scores.InsertOnSubmit(defaultScore);

                            CalculateFinalScoreAndStatus(defaultScore, sem, context, teacher.SubjectTC);
                        }
                        context.SubmitChanges();
                    }
                }

                ViewBag.IsCurrentClass = isCurrentClass;
                ViewBag.Semesters = new SelectList(semesters);
                ViewBag.SelectedSemester = semester;
                ViewBag.ClassId = classId;
                ViewBag.TeacherId = teacherId;
                ViewBag.ClassName = classInfo.ClassName;
                ViewBag.TeacherName = teacher.Name;
                ViewBag.Subject = teacher.SubjectTC;

                return View(studentScores);
            }
        }


        public ActionResult NhapDiem(int classId, int teacherId, int semester, string scoreType = "Score15")
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                if (classInfo == null)
                {
                    return HttpNotFound("Class not found.");
                }

                var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);
                if (teacher == null)
                {
                    return HttpNotFound("Teacher not found.");
                }

                var students = context.Students
                                      .Where(s => s.ClassId == classId)
                                      .Select(s => new StudentScoreViewModel
                                      {
                                          StudentId = s.StudentId,
                                          Name = s.Name,
                                          Semester = semester,
                                          ScoreType = scoreType,
                                          Score15 = context.Scores.FirstOrDefault(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester) != null
                                              ? context.Scores.FirstOrDefault(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester).Score15
                                              : (decimal?)null
                                      }).ToList();

                ViewBag.ClassId = classId;
                ViewBag.TeacherId = teacherId;
                ViewBag.Semester = semester;
                ViewBag.ScoreType = scoreType;
                ViewBag.ClassName = classInfo.ClassName;
                ViewBag.TeacherName = teacher.Name;

                return View(students);
            }
        }

        [HttpPost]
        public ActionResult SaveDiem(int classId, int teacherId, int semester, string scoreType, List<StudentScoreViewModel> students)
        {
            if (students == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Students list cannot be null");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);

                foreach (var student in students)
                {
                    if (student == null)
                    {
                        continue;
                    }

                    var score = context.Scores.FirstOrDefault(sc => sc.StudentId == student.StudentId && sc.TeacherId == teacherId && sc.Semester == semester);
                    if (score == null)
                    {
                        score = new Score
                        {
                            StudentId = student.StudentId,
                            TeacherId = teacherId,
                            Semester = semester,
                            ClassId = classId,
                            GradeID = classInfo.GradeId,
                            SubjectTC = teacher.SubjectTC
                        };
                        context.Scores.InsertOnSubmit(score);
                    }

                    if (scoreType == "Score15")
                    {
                        score.Score15 = student.Score15;
                    }
                }
                context.SubmitChanges();
            }
            return RedirectToAction("DanhSachLop", new { classId, teacherId, semester });
        }

        public ActionResult NhapDiem60(int classId, int teacherId, int semester, string scoreType = "Score60")
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                if (classInfo == null)
                {
                    return HttpNotFound("Class not found.");
                }

                var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);
                if (teacher == null)
                {
                    return HttpNotFound("Teacher not found.");
                }

                var students = context.Students
                                      .Where(s => s.ClassId == classId)
                                      .Select(s => new StudentScoreViewModel
                                      {
                                          StudentId = s.StudentId,
                                          Name = s.Name,
                                          Semester = semester,
                                          ScoreType = scoreType,
                                          Score60 = context.Scores.FirstOrDefault(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester) != null
                                              ? context.Scores.FirstOrDefault(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester).Score60
                                              : (decimal?)null
                                      }).ToList();

                ViewBag.ClassId = classId;
                ViewBag.TeacherId = teacherId;
                ViewBag.Semester = semester;
                ViewBag.ScoreType = scoreType;
                ViewBag.ClassName = classInfo.ClassName;
                ViewBag.TeacherName = teacher.Name;

                return View(students);
            }
        }

        [HttpPost]
        public ActionResult SaveDiem60(int classId, int teacherId, int semester, List<StudentScoreViewModel> students)
        {
            if (students == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Students list cannot be null");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);

                foreach (var student in students)
                {
                    if (student == null)
                    {
                        continue; // Bỏ qua các mục null
                    }

                    var score = context.Scores.FirstOrDefault(sc => sc.StudentId == student.StudentId && sc.TeacherId == teacherId && sc.Semester == semester);
                    if (score == null)
                    {
                        score = new Score
                        {
                            StudentId = student.StudentId,
                            TeacherId = teacherId,
                            Semester = semester,
                            ClassId = classId,
                            GradeID = classInfo.GradeId,
                            SubjectTC = teacher.SubjectTC
                        };
                        context.Scores.InsertOnSubmit(score);
                    }

                    score.Score60 = student.Score60;
                }
                context.SubmitChanges();
            }
            return RedirectToAction("DanhSachLop", new { classId, teacherId, semester });
        }

        public ActionResult NhapDiemGK(int classId, int teacherId, int semester, string scoreType = "GiuaKi")
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                if (classInfo == null)
                {
                    return HttpNotFound("Class not found.");
                }

                var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);
                if (teacher == null)
                {
                    return HttpNotFound("Teacher not found.");
                }

                var students = context.Students
                                      .Where(s => s.ClassId == classId)
                                      .Select(s => new StudentScoreViewModel
                                      {
                                          StudentId = s.StudentId,
                                          Name = s.Name,
                                          Semester = semester,
                                          ScoreType = scoreType,
                                          GiuaKi = context.Scores.FirstOrDefault(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester) != null
                                              ? context.Scores.FirstOrDefault(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester).GiuaKi
                                              : (decimal?)null
                                      }).ToList();

                ViewBag.ClassId = classId;
                ViewBag.TeacherId = teacherId;
                ViewBag.Semester = semester;
                ViewBag.ScoreType = scoreType;
                ViewBag.ClassName = classInfo.ClassName;
                ViewBag.TeacherName = teacher.Name;

                return View(students);
            }
        }
        [HttpPost]
        public ActionResult SaveDiemGiuaKi(int classId, int teacherId, int semester, List<StudentScoreViewModel> students)
        {
            if (students == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Students list cannot be null");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);

                foreach (var student in students)
                {
                    if (student == null)
                    {
                        continue; // Bỏ qua các mục null
                    }

                    var score = context.Scores.FirstOrDefault(sc => sc.StudentId == student.StudentId && sc.TeacherId == teacherId && sc.Semester == semester);
                    if (score == null)
                    {
                        score = new Score
                        {
                            StudentId = student.StudentId,
                            TeacherId = teacherId,
                            Semester = semester,
                            ClassId = classId,
                            GradeID = classInfo.GradeId,
                            SubjectTC = teacher.SubjectTC
                        };
                        context.Scores.InsertOnSubmit(score);
                    }

                    score.GiuaKi = student.GiuaKi;
                }
                context.SubmitChanges();
            }
            return RedirectToAction("DanhSachLop", new { classId, teacherId, semester });
        }

        public ActionResult NhapDiemCK(int classId, int teacherId, int semester, string scoreType = "CuoiKi")
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                if (classInfo == null)
                {
                    return HttpNotFound("Class not found.");
                }

                var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);
                if (teacher == null)
                {
                    return HttpNotFound("Teacher not found.");
                }

                var students = context.Students
                                      .Where(s => s.ClassId == classId)
                                      .Select(s => new StudentScoreViewModel
                                      {
                                          StudentId = s.StudentId,
                                          Name = s.Name,
                                          Semester = semester,
                                          ScoreType = scoreType,
                                          CuoiKi = context.Scores.FirstOrDefault(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester) != null
                                              ? context.Scores.FirstOrDefault(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester).CuoiKi
                                              : (decimal?)null
                                      }).ToList();

                ViewBag.ClassId = classId;
                ViewBag.TeacherId = teacherId;
                ViewBag.Semester = semester;
                ViewBag.ScoreType = scoreType;
                ViewBag.ClassName = classInfo.ClassName;
                ViewBag.TeacherName = teacher.Name;

                return View(students);
            }
        }
        [HttpPost]
        public ActionResult SaveDiemCuoiKi(int classId, int teacherId, int semester, List<StudentScoreViewModel> students)
        {
            if (students == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Students list cannot be null");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);

                foreach (var student in students)
                {
                    if (student == null)
                    {
                        continue; // Bỏ qua các mục null
                    }

                    var score = context.Scores.FirstOrDefault(sc => sc.StudentId == student.StudentId && sc.TeacherId == teacherId && sc.Semester == semester);
                    if (score == null)
                    {
                        score = new Score
                        {
                            StudentId = student.StudentId,
                            TeacherId = teacherId,
                            Semester = semester,
                            ClassId = classId,
                            GradeID = classInfo.GradeId,
                            SubjectTC = teacher.SubjectTC
                        };
                        context.Scores.InsertOnSubmit(score);
                    }

                    score.CuoiKi = student.CuoiKi;
                    CalculateFinalScoreAndStatus(score, semester, context, teacher.SubjectTC);
                }
                context.SubmitChanges();
            }
            return RedirectToAction("DanhSachLop", new { classId, teacherId, semester });
        }

        private void CalculateFinalScoreAndStatus(Score score, int semester, db_sll2DataContext context, string subjectTC)
        {
            // Extract scores list and calculate weighted scores
            var scoresList = score.Scores?.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => decimal.TryParse(s, out var num) ? (decimal?)num : null)
                .Where(n => n.HasValue)
                .Select(n => n.Value)
                .ToList() ?? new List<decimal>();

            var weightedScores = new List<decimal>();
            if (score.Score15.HasValue)
                weightedScores.Add(score.Score15.Value);
            if (score.Score60.HasValue)
                weightedScores.Add(score.Score60.Value);
            if (score.GiuaKi.HasValue)
                weightedScores.Add(score.GiuaKi.Value * 2);
            if (score.CuoiKi.HasValue)
                weightedScores.Add(score.CuoiKi.Value * 3);

            // Calculate total scores
            var allScores = scoresList.Concat(weightedScores).ToList();
            var totalScores = allScores.Sum();
            var totalElements = allScores.Count + 3;

            // Update score summary
            if (allScores.Any())
            {
                score.TongKet = Math.Round(totalScores / totalElements, 2);
                score.Status = score.TongKet >= 5 ? "Qua môn" : "Không đủ điểm";
            }
            else
            {
                score.TongKet = null;
                score.Status = "Không có điểm";
            }

            // Update or create academic result record
            var academicResult = context.AcademicResults
                .FirstOrDefault(ar => ar.StudentId == score.StudentId && ar.AcademicYear == score.GradeID);

            if (academicResult == null)
            {
                academicResult = new AcademicResult
                {
                    StudentId = score.StudentId,
                    AcademicYear = score.GradeID,
                    Status = "Đang tiến hành"
                };
                context.AcademicResults.InsertOnSubmit(academicResult);
            }

            // Assign semester scores to the relevant column and calculate final average
            decimal? sem1Score = null, sem2Score = null;
            switch (subjectTC)
            {
                case "Văn":
                    if (semester == 1)
                        sem1Score = score.TongKet;
                    else if (semester == 2)
                        sem2Score = score.TongKet;

                    if (sem1Score.HasValue || sem2Score.HasValue)
                    {
                        academicResult.AvgVietnamese = CalculateAverage(academicResult.AvgVietnamese, sem1Score, sem2Score, semester);
                    }
                    break;
                case "Toán":
                    if (semester == 1)
                        sem1Score = score.TongKet;
                    else if (semester == 2)
                        sem2Score = score.TongKet;

                    if (sem1Score.HasValue || sem2Score.HasValue)
                    {
                        academicResult.AvgMathematics = CalculateAverage(academicResult.AvgMathematics, sem1Score, sem2Score, semester);
                    }
                    break;
                case "Anh":
                    if (semester == 1)
                        sem1Score = score.TongKet;
                    else if (semester == 2)
                        sem2Score = score.TongKet;

                    if (sem1Score.HasValue || sem2Score.HasValue)
                    {
                        academicResult.AvgEnglish = CalculateAverage(academicResult.AvgEnglish, sem1Score, sem2Score, semester);
                    }
                    break;
                case "Sinh":
                    if (semester == 1)
                        sem1Score = score.TongKet;
                    else if (semester == 2)
                        sem2Score = score.TongKet;

                    if (sem1Score.HasValue || sem2Score.HasValue)
                    {
                        academicResult.AvgBiology = CalculateAverage(academicResult.AvgBiology, sem1Score, sem2Score, semester);
                    }
                    break;
                case "Lịch sử":
                    if (semester == 1)
                        sem1Score = score.TongKet;
                    else if (semester == 2)
                        sem2Score = score.TongKet;

                    if (sem1Score.HasValue || sem2Score.HasValue)
                    {
                        academicResult.AvgHistory = CalculateAverage(academicResult.AvgHistory, sem1Score, sem2Score, semester);
                    }
                    break;
                case "Địa lí":
                    if (semester == 1)
                        sem1Score = score.TongKet;
                    else if (semester == 2)
                        sem2Score = score.TongKet;

                    if (sem1Score.HasValue || sem2Score.HasValue)
                    {
                        academicResult.AvgGeography = CalculateAverage(academicResult.AvgGeography, sem1Score, sem2Score, semester);
                    }
                    break;
                case "Hóa học":
                    if (semester == 1)
                        sem1Score = score.TongKet;
                    else if (semester == 2)
                        sem2Score = score.TongKet;

                    if (sem1Score.HasValue || sem2Score.HasValue)
                    {
                        academicResult.AvgChemistry = CalculateAverage(academicResult.AvgChemistry, sem1Score, sem2Score, semester);
                    }
                    break;
                case "Công nghệ":
                    if (semester == 1)
                        sem1Score = score.TongKet;
                    else if (semester == 2)
                        sem2Score = score.TongKet;

                    if (sem1Score.HasValue || sem2Score.HasValue)
                    {
                        academicResult.AvgTechnology = CalculateAverage(academicResult.AvgTechnology, sem1Score, sem2Score, semester);
                    }
                    break;
            }

            context.SubmitChanges();
        }

        private decimal? CalculateAverage(decimal? currentAvg, decimal? sem1Score, decimal? sem2Score, int semester)
        {
            if (semester == 1 && sem1Score.HasValue)
            {
                return Math.Round(sem1Score.Value, 2);
            }
            else if (semester == 2 && currentAvg.HasValue && sem2Score.HasValue)
            {
                return Math.Round((currentAvg.Value + sem2Score.Value * 2) / 3, 2);
            }
            else if (semester == 2 && sem2Score.HasValue)
            {
                return Math.Round(sem2Score.Value, 2);
            }
            return currentAvg.HasValue ? Math.Round(currentAvg.Value, 2) : currentAvg;
        }

        public ActionResult NhapDiemThuongXuyen(int classId, int teacherId, int semester = 1)
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                if (classInfo == null)
                {
                    return HttpNotFound("Class not found.");
                }

                var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);
                if (teacher == null)
                {
                    return HttpNotFound("Teacher not found.");
                }

                var students = context.Students
                                      .Where(s => s.ClassId == classId)
                                      .Select(s => new StudentScoreViewModel
                                      {
                                          StudentId = s.StudentId,
                                          Name = s.Name,
                                          Scores = context.Scores.Where(sc => sc.StudentId == s.StudentId && sc.TeacherId == teacherId && sc.Semester == semester).Select(sc => sc.Scores).FirstOrDefault(),
                                          Semester = semester,
                                      }).ToList();

                ViewBag.ClassId = classId;
                ViewBag.TeacherId = teacherId;
                ViewBag.Semester = semester;
                ViewBag.ClassName = classInfo.ClassName;
                ViewBag.TeacherName = teacher.Name;

                return View(students);
            }
        }
        [HttpPost]
        public ActionResult SaveDiemThuongXuyen(int classId, int teacherId, int semester, List<StudentScoreViewModel> students)
        {
            if (students == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Students list cannot be null");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == classId);
                var teacher = context.Teachers.FirstOrDefault(t => t.TeacherId == teacherId);
                if (classInfo == null || teacher == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Class or Teacher not found");
                }

                foreach (var student in students)
                {
                    if (student == null)
                    {
                        continue;
                    }

                    var score = context.Scores.FirstOrDefault(sc => sc.StudentId == student.StudentId && sc.TeacherId == teacherId && sc.Semester == semester);
                    if (score == null)
                    {
                        score = new Score
                        {
                            StudentId = student.StudentId,
                            TeacherId = teacherId,
                            Semester = semester,
                            ClassId = classId,
                            GradeID = classInfo.GradeId,
                            SubjectTC = teacher.SubjectTC,
                            Scores = student.Scores != null ? string.Join(",", student.Scores) : null
                        };
                        context.Scores.InsertOnSubmit(score);
                    }
                    else
                    {
                        score.Scores = student.Scores != null ? string.Join(",", student.Scores) : null;
                    }
                }

                try
                {
                    context.SubmitChanges();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error: " + ex.Message);
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Error saving to database");
                }
            }
            return RedirectToAction("DanhSachLop", new { classId = classId, teacherId = teacherId, semester = semester });
        }

        private decimal? ConvertStringToDecimal(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            else
            {
                // Try parsing the string to decimal
                if (decimal.TryParse(value, out decimal result))
                {
                    return result;
                }
                else
                {
                    // Parsing failed, return null
                    return null;
                }
            }
        }
        public ActionResult ChiTietDiemGV(int studentId, int classId, int? semester, int? academicYear)
        {
            var user = Session["khach"] as User;
            if (user == null || user.UserType != 2)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var teacher = context.Teachers.FirstOrDefault(t => t.UserId == user.UserId);
                if (teacher == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                var teachingClass = context.Classes.FirstOrDefault(c => c.ClassId == classId && c.TeacherId == teacher.TeacherId);
                if (teachingClass == null)
                {
                    return HttpNotFound("Bạn không dạy lớp này.");
                }

                var student = context.Students.FirstOrDefault(s => s.StudentId == studentId && s.ClassId == classId);
                if (student == null)
                {
                    return HttpNotFound("Không tìm thấy sinh viên.");
                }

                var scoresQuery = context.Scores
                    .Where(s => s.StudentId == studentId && s.ClassId == classId);

                var selectedClass = context.Classes.FirstOrDefault(c => c.ClassId == classId);

                if (selectedClass == null)
                {
                    return HttpNotFound("Không tìm thấy lớp.");
                }

                if (semester.HasValue)
                {
                    scoresQuery = scoresQuery.Where(s => s.Semester == semester.Value);
                }

                var scores = scoresQuery.ToList();

                ViewBag.StudentName = student.Name;
                ViewBag.ClassName = context.Classes.FirstOrDefault(c => c.ClassId == classId)?.ClassName;

                var semesters = new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "Kỳ 1" },
            new SelectListItem { Value = "2", Text = "Kỳ 2" }
        };
                ViewBag.Semesters = new SelectList(semesters, "Value", "Text");


                return View(scores);
            }
        }
        public ActionResult ChiTietLopHoc(int id)
        {
            try
            {
                var user = Session["khach"] as User;
                if (user == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var classInfo = context.Classes.FirstOrDefault(c => c.ClassId == id);
                    if (classInfo == null)
                    {
                        return HttpNotFound();
                    }

                    var students = context.Students.Where(s => s.ClassId == classInfo.ClassId).ToList();

                    var teacherName = context.Teachers.FirstOrDefault(t => t.TeacherId == classInfo.TeacherId)?.Name;

                    ViewBag.ClassInfo = classInfo;
                    ViewBag.Students = students;
                    ViewBag.TeacherName = teacherName;

                    return View();
                }
            }
            catch (Exception ex)
            {
                return View();
            }
        }
        public ActionResult PhuHuynh()
        {
            try
            {
                var user = Session["khach"] as User;
                if (user == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var parent = context.Parents.FirstOrDefault(p => p.UserId == user.UserId);
                    if (parent == null)
                    {
                        return RedirectToAction("DangNhap", "Home");
                    }

                    var students = context.ParentStudentRelationships
                                          .Where(ps => ps.ParentId == parent.ParentId)
                                          .Select(ps => new
                                          {
                                              Student = ps.Student,
                                              ClassName = context.Classes.FirstOrDefault(c => c.ClassId == ps.Student.ClassId).ClassName,
                                              HomeroomTeacher = context.Teachers.FirstOrDefault(t => t.TeacherId == context.Classes.FirstOrDefault(c => c.ClassId == ps.Student.ClassId).TeacherId).Name
                                          })
                                          .ToList();

                    var studentModels = students.Select(s => new StudentViewModel
                    {
                        StudentId = s.Student.StudentId,
                        UserId = s.Student.UserId ?? default(int),
                        Name = s.Student.Name,
                        DateOfBirth = s.Student.DateOfBirth,
                        ClassId = s.Student.ClassId ?? default(int),
                        SDTPH = s.Student.SDTPH,
                        GradeID = s.Student.GradeID ?? default(int),
                        ClassName = s.ClassName,
                        HomeroomTeacher = s.HomeroomTeacher
                    }).ToList();

                    return View(studentModels);
                }
            }
            catch (Exception ex)
            {

            }

            return View(new List<StudentViewModel>());
        }
        public ActionResult ThongTinNguoiDung()
        {
            var user = Session["khach"] as User;
            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var parent = context.Parents.FirstOrDefault(p => p.UserId == user.UserId);
                if (parent == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                var studentPhone = context.Students
                                         .Where(s => s.UserId == user.UserId)
                                         .Select(s => s.SDTPH)
                                         .FirstOrDefault();

                ViewBag.StudentPhone = studentPhone;

                return View(parent);
            }
        }

        [HttpPost]
        public ActionResult ThongTinNguoiDung(Parent model)
        {
            var user = Session["khach"] as User;
            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var parent = context.Parents.FirstOrDefault(p => p.UserId == user.UserId);
                if (parent != null)
                {
                    parent.Name = model.Name;
                    parent.Email = model.Email;
                    parent.Address = model.Address;
                    context.SubmitChanges();
                }
            }

            return RedirectToAction("ThongTinNguoiDung");
        }


        public ActionResult ChiTietHocSinh(int id)
        {
            try
            {
                var user = Session["khach"] as User;
                if (user == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }
                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var student = context.Students.FirstOrDefault(s => s.UserId == id);
                    if (student == null)
                    {
                        return HttpNotFound();
                    }
                    var className = context.Classes.FirstOrDefault(c => c.ClassId == student.ClassId)?.ClassName;
                    ViewBag.ClassName = className;


                    return View(student);
                }
            }
            catch (Exception ex)
            {
            }

            return View();
        }
        public ActionResult ThongTinGiaoVien()
        {
            var user = Session["khach"] as User;
            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var teacher = context.Teachers.FirstOrDefault(t => t.UserId == user.UserId);
                if (teacher == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                ViewBag.Subjects = context.SubjectTeachers
                                          .Where(st => st.TeacherId == teacher.TeacherId)
                                          .Select(st => st.Subject.SubjectName)
                                          .ToList();

                return View(teacher);
            }
        }


        [HttpPost]
        public ActionResult ThongTinGiaoVien(Teacher model)
        {
            var user = Session["khach"] as User;
            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var teacher = context.Teachers.FirstOrDefault(t => t.UserId == user.UserId);
                if (teacher != null)
                {
                    teacher.Name = model.Name;
                    teacher.SDT = model.SDT;
                    teacher.Address = model.Address;
                    context.SubmitChanges();
                }
            }

            return RedirectToAction("ThongTinGiaoVien");
        }


        public ActionResult DanhSachDonXinPhepPhuHuynh(int studentId)
        {
            var user = Session["khach"] as User;
            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var parent = context.Parents.FirstOrDefault(p => p.UserId == user.UserId);
                if (parent == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                var student = context.Students.FirstOrDefault(s => s.StudentId == studentId);
                if (student == null)
                {
                    return HttpNotFound();
                }

                var leaveRequests = context.LeaveRequests
                    .Where(lr => lr.StudentId == studentId)
                    .Select(lr => new ParentLeaveRequestViewModel
                    {
                        RequestId = lr.RequestId,
                        StudentName = student.Name,
                        ClassName = context.Classes.FirstOrDefault(c => c.ClassId == student.ClassId).ClassName,
                        Reason = lr.Reason,
                        RequestDate = lr.RequestDate,
                        ApprovalStatus = lr.ApprovalStatus
                    })
                    .ToList();

                ViewBag.ParentName = parent.Name;
                ViewBag.StudentName = student.Name;

                return View(leaveRequests);
            }
        }
        public ActionResult XemThoiKhoaBieu(int id, DateTime? date, bool schedule = false, bool exam = false, bool both = false, int weekOffset = 0)
        {
            try
            {
                var user = Session["khach"] as User;
                if (user == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var student = context.Students.FirstOrDefault(s => s.UserId == id);
                    if (student == null)
                    {
                        return HttpNotFound();
                    }

                    if (!date.HasValue)
                    {
                        date = DateTime.Now;
                    }

                    date = date.Value.AddDays(weekOffset * 7);
                    var startDate = date.Value.AddDays(-(int)date.Value.DayOfWeek + (int)DayOfWeek.Monday);

                    IQueryable<Timetable> timetableQuery = context.Timetables
                        .Where(t => t.ClassId == student.ClassId && t.Date >= startDate && t.Date < startDate.AddDays(7));

                    var timetable = timetableQuery
                        .OrderBy(t => t.Weekdays)
                        .ThenBy(t => t.Times)
                        .Select(t => new TimetableDetailViewModel
                        {
                            Weekday = t.Weekdays ?? default(int),
                            Time = t.Times,
                            SubjectName = context.SubjectTeachers
                                                .Where(st => st.TeacherId == t.TeacherId)
                                                .Join(context.Subjects,
                                                      st => st.SubjectId,
                                                      s => s.SubjectId,
                                                      (st, s) => s.SubjectName)
                                                .FirstOrDefault(),
                            TeacherName = context.Teachers
                                                 .Where(te => te.TeacherId == t.TeacherId)
                                                 .Select(te => te.Name)
                                                 .FirstOrDefault()
                        })
                        .ToList();

                    var viewModel = new TimetableViewModel
                    {
                        Student = student,
                        TimetableDetails = timetable,
                        StartDate = startDate
                    };

                    ViewData["CurrentDate"] = date;

                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                // Log exception or handle it as needed
                throw;
            }
        }
        public ActionResult TaoDonXinPhep(int studentId)
        {
            try
            {
                var user = Session["khach"] as User;
                if (user == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var student = context.Students.FirstOrDefault(s => s.StudentId == studentId);
                    if (student == null)
                    {
                        return HttpNotFound();
                    }

                    var headTeacher = context.TeacherClassAssignments
                                        .Where(t => t.ClassId == student.ClassId && t.IsHeadTeacher == 1)
                                        .Select(t => t.Teacher)
                                        .FirstOrDefault();

                    if (headTeacher == null)
                    {
                        ViewBag.ErrorMessage = "Không tìm thấy giáo viên chủ nhiệm cho lớp học này.";
                        return View("Error");
                    }

                    ViewBag.Teachers = new SelectList(new List<Teacher> { headTeacher }, "TeacherId", "Name");

                    return View(student);
                }
            }
            catch (Exception ex)
            {

                return View("Error");
            }
        }
        [HttpPost]
        public ActionResult TaoDonXinPhep(int studentId, int teacherId, string reason)
        {
            try
            {
                var user = Session["khach"] as User;
                if (user == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var leaveRequest = new LeaveRequest
                    {
                        StudentId = studentId,
                        TeacherId = teacherId,
                        Reason = reason,
                        RequestDate = DateTime.Now,
                        ApprovalStatus = "Chưa giải quyết"
                    };

                    context.LeaveRequests.InsertOnSubmit(leaveRequest);
                    context.SubmitChanges();

                    return RedirectToAction("PhuHuynh");
                }
            }
            catch (Exception ex)
            {

            }

            return RedirectToAction("TaoDonXinPhep", new { studentId = studentId });
        }
        public ActionResult DanhSachDonXinPhep(int? teacherId)
        {
            var user = Session["khach"] as User;
            if (user == null || user.UserType != 2)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var teacher = context.Teachers.FirstOrDefault(t => t.UserId == user.UserId);
                if (teacher == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                var leaveRequests = context.LeaveRequests
                    .Where(lr => lr.TeacherId == teacher.TeacherId)
                    .ToList();

                var studentIds = leaveRequests.Select(lr => lr.StudentId).Distinct().ToList();
                var students = context.Students.Where(s => studentIds.Contains(s.StudentId)).ToList();
                var classIds = students.Select(s => s.ClassId).Distinct().ToList();
                var classes = context.Classes.Where(c => classIds.Contains(c.ClassId)).ToList();

                var leaveRequestViewModels = leaveRequests.Select(lr => {
                    var student = students.FirstOrDefault(s => s.StudentId == lr.StudentId);
                    var className = student != null ? classes.FirstOrDefault(c => c.ClassId == student.ClassId)?.ClassName : "Không rõ";
                    return new LeaveRequestViewModel
                    {
                        RequestId = lr.RequestId,
                        StudentName = student?.Name ?? "Không rõ",
                        ClassName = className,
                        Reason = lr.Reason,
                        RequestDate = lr.RequestDate,
                        ApprovalStatus = lr.ApprovalStatus
                    };
                }).ToList();

                ViewBag.TeacherName = teacher.Name;

                return View(leaveRequestViewModels);
            }
        }
        [HttpPost]
        public ActionResult UpdateApprovalStatus(int requestId, string approvalStatus)
        {
            using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
            {
                var leaveRequest = context.LeaveRequests.FirstOrDefault(lr => lr.RequestId == requestId);
                if (leaveRequest != null)
                {
                    leaveRequest.ApprovalStatus = approvalStatus;
                    context.SubmitChanges();
                }
            }
            return Json(new { success = true });
        }
        public class ScoreViewModel
        {
            public int? StudentId { get; set; }
            public string SubjectTC { get; set; }
            public string Scores { get; set; }
            public decimal? Score15 { get; set; }
            public decimal? Score60 { get; set; }
            public decimal? GiuaKi { get; set; }
            public decimal? CuoiKi { get; set; }
            public decimal? TongKet { get; set; }
            public string Status { get; set; }
            public int? Semester { get; set; }
            public int? AcademicYear { get; set; }
        }

        public ActionResult ChiTietDiem(int id, int? semester, int? gradeId)
        {
            try
            {
                var user = Session["khach"] as User;
                if (user == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var scoresQuery = context.Scores.Where(s => s.StudentId == id);

                    if (semester.HasValue)
                    {
                        scoresQuery = scoresQuery.Where(s => s.Semester == semester.Value);
                    }

                    if (gradeId.HasValue)
                    {
                        scoresQuery = scoresQuery.Where(s => s.GradeID == gradeId.Value);
                    }

                    var scores = scoresQuery
                        .Join(context.Grades, s => s.GradeID, g => g.GradeID, (s, g) => new ScoreViewModel
                        {
                            StudentId = s.StudentId,
                            SubjectTC = s.SubjectTC,
                            Scores = s.Scores,
                            Score15 = s.Score15,
                            Score60 = s.Score60,
                            GiuaKi = s.GiuaKi,
                            CuoiKi = s.CuoiKi,
                            TongKet = s.TongKet,
                            Status = s.Status,
                            Semester = s.Semester,
                            AcademicYear = g.AcademicYear
                        })
                        .ToList();

                    var semesters = scores.Select(s => s.Semester).Distinct().OrderBy(s => s);
                    ViewBag.Semesters = new SelectList(semesters);

                    var academicYears = scores.Select(s => s.AcademicYear).Distinct().OrderBy(y => y);
                    ViewBag.AcademicYears = new SelectList(academicYears);

                    ViewBag.SelectedSemester = semester;
                    ViewBag.SelectedGradeId = gradeId;

                    return View(scores);
                }
            }
            catch (Exception ex)
            {
                return View("Error", new HandleErrorInfo(ex, "Home", "ChiTietDiem"));
            }
        }




        public class ScoreUpdateModel
        {
            public string SubjectTC { get; set; }
            public decimal TongKet { get; set; }
            public int StudentId { get; set; }
            public int GradeID { get; set; }
            public int Semester { get; set; }
        }



        [HttpPost]
        public ActionResult UpdateScores(List<ScoreUpdateModel> scores)
        {
            try
            {
                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    foreach (var score in scores)
                    {
                        var existingScore = context.Scores.FirstOrDefault(s => s.SubjectTC == score.SubjectTC && s.StudentId == score.StudentId && s.GradeID == score.GradeID && s.Semester == score.Semester);
                        if (existingScore != null)
                        {
                            existingScore.TongKet = score.TongKet;
                        }
                        else
                        {
                            return Json(new { success = false, message = $"Score record not found for Subject: {score.SubjectTC}, Student: {score.StudentId}, Grade: {score.GradeID}, Semester: {score.Semester}." });
                        }
                    }
                    context.SubmitChanges();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }



        public ActionResult Error()
        {
            return View();
        }
        public ActionResult erorrhehe()
        {
            return View();
        }
        public ActionResult erhehe()
        {
            return View();
        }
        public ActionResult thongBao()
        {
            var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True");
            var notifications = context.Notifications.ToList();
            return View(notifications);
        }
        public ActionResult ThongBaoGiaoVien()
        {

            var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True");
            var teacherNotifications = context.Notifications
                .Where(n => n.UserType == 2)
                .ToList();
            return View(teacherNotifications);
        }
        public ActionResult ThongBaoPhuHuynh()
        {
            var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True");
            var parentNotifications = context.Notifications
                .Where(n => n.UserType == 1)
                .ToList();
            return View(parentNotifications);
        }

        public ActionResult TaoDonXinPhepGiaoVien()
        {
            try
            {
                var user = Session["khach"] as User;
                if (user == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var teacher = context.Teachers.FirstOrDefault(t => t.UserId == user.UserId);
                    if (teacher == null)
                    {
                        return HttpNotFound();
                    }

                    return View(teacher);
                }
            }
            catch (Exception ex)
            {
                return View("Error");
            }
        }

        [HttpPost]
        public ActionResult TaoDonXinPhepGiaoVien(int teacherId, string reason)
        {
            try
            {
                var user = Session["khach"] as User;
                if (user == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var leaveRequest = new LeaveRequest
                    {
                        TeacherId = teacherId,
                        Reason = reason,
                        RequestDate = DateTime.Now,
                        ApprovalStatus = "Chưa giải quyết"
                    };

                    context.LeaveRequests.InsertOnSubmit(leaveRequest);
                    context.SubmitChanges();

                    return RedirectToAction("GiaoVien");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("TaoDonXinPhepGiaoVien");
            }
        }
        public ActionResult DanhSachDonXinPhepGiaoVien(int teacherId)
        {
            try
            {
                using (var context = new db_sll2DataContext("Data Source=Fate;Initial Catalog=soLienLac5;Integrated Security=True"))
                {
                    var leaveRequests = context.LeaveRequests
                                               .Where(lr => lr.TeacherId == teacherId)
                                               .OrderByDescending(lr => lr.RequestDate)
                                               .ToList();

                    if (!leaveRequests.Any())
                    {
                        ViewBag.Message = "Không có đơn xin phép nào.";
                        return View(new List<LeaveRequest>());
                    }

                    return View(leaveRequests);
                }
            }
            catch (Exception ex)
            {
                return View("Error");
            }
        }
        



    }
}
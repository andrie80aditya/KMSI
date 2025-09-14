using KMSI.Data;
using KMSI.Models.ViewModels;
using KMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize(Policy = "BranchManagerAndAbove")]
    public class ScheduleController : BaseController
    {
        public ScheduleController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var siteId = GetCurrentSiteId();
            var userLevel = GetCurrentUserLevel();

            var schedulesQuery = _context.ClassSchedules
                .Include(cs => cs.Company)
                .Include(cs => cs.Site)
                .Include(cs => cs.Student)
                .Include(cs => cs.Teacher)
                .ThenInclude(t => t.User)
                .Include(cs => cs.Grade)
                .Where(cs => cs.CompanyId == companyId);

            // Filter by site if user is not HO Admin or above
            if (!IsHOAdmin() && siteId.HasValue)
            {
                schedulesQuery = schedulesQuery.Where(cs => cs.SiteId == siteId.Value);
            }

            var schedules = await schedulesQuery
                .Where(cs => cs.ScheduleDate >= DateOnly.FromDateTime(DateTime.Today.AddDays(-7))) // Show from last week
                .OrderBy(cs => cs.ScheduleDate)
                .ThenBy(cs => cs.StartTime)
                .ToListAsync();

            ViewBag.Companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            ViewBag.Sites = await _context.Sites
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .OrderBy(s => s.SiteName)
                .ToListAsync();

            ViewBag.Students = await _context.Students
                .Where(s => s.CompanyId == companyId && s.IsActive && s.Status == "Active")
                .OrderBy(s => s.FirstName)
                .ToListAsync();

            ViewBag.Teachers = await _context.Teachers
                .Include(t => t.User)
                .Where(t => t.CompanyId == companyId && t.IsActive)
                .OrderBy(t => t.User.FirstName)
                .ToListAsync();

            ViewBag.Grades = await _context.Grades
                .Where(g => g.CompanyId == companyId && g.IsActive)
                .OrderBy(g => g.SortOrder ?? int.MaxValue)
                .ThenBy(g => g.GradeName)
                .ToListAsync();

            ViewData["Breadcrumb"] = "Schedule Management";
            return View(schedules);
        }

        [HttpGet]
        public async Task<IActionResult> GetSchedule(int id)
        {
            var schedule = await _context.ClassSchedules
                .Include(cs => cs.Company)
                .Include(cs => cs.Site)
                .Include(cs => cs.Student)
                .Include(cs => cs.Teacher)
                .Include(cs => cs.Grade)
                .FirstOrDefaultAsync(cs => cs.ClassScheduleId == id);

            if (schedule == null)
                return NotFound();

            return Json(new
            {
                classScheduleId = schedule.ClassScheduleId,
                companyId = schedule.CompanyId,
                siteId = schedule.SiteId,
                studentId = schedule.StudentId,
                teacherId = schedule.TeacherId,
                gradeId = schedule.GradeId,
                scheduleDate = schedule.ScheduleDate.ToString("yyyy-MM-dd"),
                startTime = schedule.StartTime.ToString(@"hh\:mm"),
                endTime = schedule.EndTime.ToString(@"hh\:mm"),
                duration = schedule.Duration,
                scheduleType = schedule.ScheduleType,
                status = schedule.Status,
                room = schedule.Room,
                notes = schedule.Notes,
                isRecurring = schedule.IsRecurring,
                recurrencePattern = schedule.RecurrencePattern
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(ScheduleViewModel model)
        {
            try
            {
                // Validate time
                if (model.EndTime <= model.StartTime)
                {
                    return Json(new { success = false, message = "End time must be after start time." });
                }

                // Check for scheduling conflicts
                var hasConflict = await CheckScheduleConflict(
                    model.TeacherId,
                    model.StudentId,
                    model.ScheduleDate,
                    model.StartTime,
                    model.EndTime,
                    null);

                if (hasConflict.HasConflict)
                {
                    return Json(new { success = false, message = hasConflict.Message });
                }

                // Calculate duration
                var duration = (int)(model.EndTime - model.StartTime).TotalMinutes;

                var schedule = new ClassSchedule
                {
                    CompanyId = model.CompanyId,
                    SiteId = model.SiteId,
                    StudentId = model.StudentId,
                    TeacherId = model.TeacherId,
                    GradeId = model.GradeId,
                    ScheduleDate = DateOnly.FromDateTime(model.ScheduleDate),
                    StartTime = TimeOnly.FromTimeSpan(model.StartTime),
                    EndTime = TimeOnly.FromTimeSpan(model.EndTime),
                    Duration = duration,
                    ScheduleType = model.ScheduleType,
                    Status = model.Status,
                    Room = model.Room,
                    Notes = model.Notes,
                    IsRecurring = model.IsRecurring,
                    RecurrencePattern = model.RecurrencePattern,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.ClassSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                // Generate recurring schedules if enabled
                if (model.IsRecurring && !string.IsNullOrEmpty(model.RecurrencePattern))
                {
                    await GenerateRecurringSchedules(schedule);
                }

                return Json(new { success = true, message = "Schedule created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating schedule: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(ScheduleViewModel model)
        {
            try
            {
                var schedule = await _context.ClassSchedules.FindAsync(model.ClassScheduleId);
                if (schedule == null)
                {
                    return Json(new { success = false, message = "Schedule not found." });
                }

                // Validate time
                if (model.EndTime <= model.StartTime)
                {
                    return Json(new { success = false, message = "End time must be after start time." });
                }

                // Check for scheduling conflicts (excluding current schedule)
                var hasConflict = await CheckScheduleConflict(
                    model.TeacherId,
                    model.StudentId,
                    model.ScheduleDate,
                    model.StartTime,
                    model.EndTime,
                    model.ClassScheduleId);

                if (hasConflict.HasConflict)
                {
                    return Json(new { success = false, message = hasConflict.Message });
                }

                // Calculate duration
                var duration = (int)(model.EndTime - model.StartTime).TotalMinutes;

                schedule.CompanyId = model.CompanyId;
                schedule.SiteId = model.SiteId;
                schedule.StudentId = model.StudentId;
                schedule.TeacherId = model.TeacherId;
                schedule.GradeId = model.GradeId;
                schedule.ScheduleDate = DateOnly.FromDateTime(model.ScheduleDate);
                schedule.StartTime = TimeOnly.FromTimeSpan(model.StartTime);
                schedule.EndTime = TimeOnly.FromTimeSpan(model.EndTime);
                schedule.Duration = duration;
                schedule.ScheduleType = model.ScheduleType;
                schedule.Status = model.Status;
                schedule.Room = model.Room;
                schedule.Notes = model.Notes;
                schedule.IsRecurring = model.IsRecurring;
                schedule.RecurrencePattern = model.RecurrencePattern;
                schedule.UpdatedBy = GetCurrentUserId();
                schedule.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Schedule updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating schedule: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var schedule = await _context.ClassSchedules.FindAsync(id);
                if (schedule == null)
                {
                    return Json(new { success = false, message = "Schedule not found." });
                }

                // Check if schedule has attendance records
                var hasAttendance = await _context.Attendances.AnyAsync(a => a.ClassScheduleId == id);
                if (hasAttendance)
                {
                    return Json(new { success = false, message = "Cannot delete schedule that has attendance records." });
                }

                // Check if schedule is in the past and was completed
                if (schedule.ScheduleDate < DateOnly.FromDateTime(DateTime.Today) && schedule.Status == "Completed")
                {
                    return Json(new { success = false, message = "Cannot delete completed schedule from the past." });
                }

                // Cancel instead of delete if schedule is today or in the future
                if (schedule.ScheduleDate >= DateOnly.FromDateTime(DateTime.Today))
                {
                    schedule.Status = "Cancelled";
                    schedule.UpdatedBy = GetCurrentUserId();
                    schedule.UpdatedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Schedule cancelled successfully." });
                }

                // Hard delete for old schedules without attendance
                _context.ClassSchedules.Remove(schedule);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Schedule deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting schedule: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSitesByCompany(int companyId)
        {
            var sites = await _context.Sites
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .Select(s => new { value = s.SiteId, text = s.SiteName })
                .OrderBy(s => s.text)
                .ToListAsync();

            return Json(sites);
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentsBySite(int siteId)
        {
            try
            {
                var students = await _context.Students
                    .Where(s => s.SiteId == siteId && s.IsActive && s.Status == "Active")
                    .Select(s => new
                    {
                        StudentId = s.StudentId,
                        FirstName = s.FirstName,
                        LastName = s.LastName,
                        StudentCode = s.StudentCode,
                        CurrentGradeId = s.CurrentGradeId
                    })
                    .ToListAsync();

                var result = students.Select(s => new
                {
                    value = s.StudentId,
                    text = $"{s.FirstName} {s.LastName} ({s.StudentCode})",
                    gradeId = s.CurrentGradeId
                }).OrderBy(s => s.text).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                string err = ex.Message;
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTeachersBySite(int siteId)
        {
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .Where(t => t.SiteId == siteId && t.IsActive)
                .Select(t => new {
                    TeacherId = t.TeacherId,
                    FirstName = t.User.FirstName,
                    LastName = t.User.LastName,
                    TeacherCode = t.TeacherCode
                })
                .ToListAsync();

            var result = teachers.Select(t => new {
                value = t.TeacherId,
                text = $"{t.FirstName} {t.LastName} ({t.TeacherCode})"
            }).OrderBy(t => t.text).ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetGradesByCompany(int companyId)
        {
            var grades = await _context.Grades
                .Where(g => g.CompanyId == companyId && g.IsActive)
                .Select(g => new { value = g.GradeId, text = g.GradeName })
                .OrderBy(g => g.text)
                .ToListAsync();

            return Json(grades);
        }

        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int teacherId, int studentId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeScheduleId = null)
        {
            var conflict = await CheckScheduleConflict(teacherId, studentId, date, startTime, endTime, excludeScheduleId);
            return Json(new { hasConflict = conflict.HasConflict, message = conflict.Message });
        }

        private async Task<(bool HasConflict, string Message)> CheckScheduleConflict(int teacherId, int studentId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeScheduleId)
        {
            var scheduleDate = DateOnly.FromDateTime(date);
            var startTimeOnly = TimeOnly.FromTimeSpan(startTime);
            var endTimeOnly = TimeOnly.FromTimeSpan(endTime);

            // Check teacher availability
            var teacherConflict = await _context.ClassSchedules
                .Where(cs => cs.TeacherId == teacherId
                    && cs.ScheduleDate == scheduleDate
                    && cs.Status != "Cancelled"
                    && (excludeScheduleId == null || cs.ClassScheduleId != excludeScheduleId)
                    && ((cs.StartTime < endTimeOnly && cs.EndTime > startTimeOnly)))
                .AnyAsync();

            if (teacherConflict)
            {
                return (true, "Teacher is not available at the selected time.");
            }

            // Check student availability
            var studentConflict = await _context.ClassSchedules
                .Where(cs => cs.StudentId == studentId
                    && cs.ScheduleDate == scheduleDate
                    && cs.Status != "Cancelled"
                    && (excludeScheduleId == null || cs.ClassScheduleId != excludeScheduleId)
                    && ((cs.StartTime < endTimeOnly && cs.EndTime > startTimeOnly)))
                .AnyAsync();

            if (studentConflict)
            {
                return (true, "Student already has a class at the selected time.");
            }

            return (false, string.Empty);
        }

        private async Task GenerateRecurringSchedules(ClassSchedule baseSchedule)
        {
            // Simple weekly recurrence for 4 weeks
            if (baseSchedule.RecurrencePattern?.ToLower().Contains("weekly") == true)
            {
                var recurringSchedules = new List<ClassSchedule>();

                for (int week = 1; week <= 4; week++)
                {
                    var nextDate = baseSchedule.ScheduleDate.AddDays(7 * week);

                    // Check if there's a conflict for this recurring schedule
                    var hasConflict = await CheckScheduleConflict(
                        baseSchedule.TeacherId,
                        baseSchedule.StudentId,
                        nextDate.ToDateTime(TimeOnly.MinValue),
                        baseSchedule.StartTime.ToTimeSpan(),
                        baseSchedule.EndTime.ToTimeSpan(),
                        null
                    );

                    if (!hasConflict.HasConflict)
                    {
                        var recurringSchedule = new ClassSchedule
                        {
                            CompanyId = baseSchedule.CompanyId,
                            SiteId = baseSchedule.SiteId,
                            StudentId = baseSchedule.StudentId,
                            TeacherId = baseSchedule.TeacherId,
                            GradeId = baseSchedule.GradeId,
                            ScheduleDate = nextDate,
                            StartTime = baseSchedule.StartTime,
                            EndTime = baseSchedule.EndTime,
                            Duration = baseSchedule.Duration,
                            ScheduleType = baseSchedule.ScheduleType,
                            Status = "Scheduled",
                            Room = baseSchedule.Room,
                            Notes = baseSchedule.Notes,
                            IsRecurring = false, // Individual recurring instances
                            RecurrencePattern = null,
                            CreatedBy = baseSchedule.CreatedBy,
                            CreatedDate = DateTime.Now
                        };

                        recurringSchedules.Add(recurringSchedule);
                    }
                }

                if (recurringSchedules.Any())
                {
                    _context.ClassSchedules.AddRange(recurringSchedules);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}

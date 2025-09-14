using KMSI.Data;
using KMSI.Models;
using KMSI.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize(Policy = "TeacherAndAbove")]
    public class AttendanceController : BaseController
    {
        public AttendanceController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var siteId = GetCurrentSiteId();
            var userLevel = GetCurrentUserLevel();
            var currentUserId = GetCurrentUserId();

            var attendancesQuery = _context.Attendances
                .Include(a => a.ClassSchedule)
                .ThenInclude(cs => cs.Site)
                .Include(a => a.Student)
                .Include(a => a.Teacher)
                .ThenInclude(t => t.User)
                .Where(a => a.ClassSchedule.CompanyId == companyId);

            // Filter by site if user is not HO Admin or above
            if (!IsHOAdmin() && siteId.HasValue)
            {
                attendancesQuery = attendancesQuery.Where(a => a.ClassSchedule.SiteId == siteId.Value);
            }

            // Filter by teacher if user is a teacher
            if (userLevel == "TEACHER")
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == currentUserId);
                if (teacher != null)
                {
                    attendancesQuery = attendancesQuery.Where(a => a.TeacherId == teacher.TeacherId);
                }
            }

            var attendances = await attendancesQuery
                .OrderByDescending(a => a.AttendanceDate)
                .ThenByDescending(a => a.ActualStartTime)
                .ToListAsync();

            // Get scheduled classes for attendance taking
            var scheduledClassesQuery = _context.ClassSchedules
                .Include(cs => cs.Company)
                .Include(cs => cs.Site)
                .Include(cs => cs.Student)
                .Include(cs => cs.Teacher)
                .ThenInclude(t => t.User)
                .Include(cs => cs.Grade)
                .Where(cs => cs.CompanyId == companyId
                    && cs.Status == "Scheduled"
                    && cs.ScheduleDate >= DateOnly.FromDateTime(DateTime.Today.AddDays(-1))); // Show from yesterday

            if (!IsHOAdmin() && siteId.HasValue)
            {
                scheduledClassesQuery = scheduledClassesQuery.Where(cs => cs.SiteId == siteId.Value);
            }

            if (userLevel == "TEACHER")
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == currentUserId);
                if (teacher != null)
                {
                    scheduledClassesQuery = scheduledClassesQuery.Where(cs => cs.TeacherId == teacher.TeacherId);
                }
            }

            var scheduledClasses = await scheduledClassesQuery
                .OrderBy(cs => cs.ScheduleDate)
                .ThenBy(cs => cs.StartTime)
                .ToListAsync();

            ViewBag.ScheduledClasses = scheduledClasses;

            ViewData["Breadcrumb"] = "Attendance Management";
            return View(attendances);
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendance(int id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.ClassSchedule)
                .Include(a => a.Student)
                .Include(a => a.Teacher)
                .FirstOrDefaultAsync(a => a.AttendanceId == id);

            if (attendance == null)
                return NotFound();

            return Json(new
            {
                attendanceId = attendance.AttendanceId,
                classScheduleId = attendance.ClassScheduleId,
                studentId = attendance.StudentId,
                teacherId = attendance.TeacherId,
                attendanceDate = attendance.AttendanceDate.ToString("yyyy-MM-dd"),
                actualStartTime = attendance.ActualStartTime?.ToString(@"hh\:mm"),
                actualEndTime = attendance.ActualEndTime?.ToString(@"hh\:mm"),
                status = attendance.Status,
                lessonTopic = attendance.LessonTopic,
                studentProgress = attendance.StudentProgress,
                teacherNotes = attendance.TeacherNotes,
                homeworkAssigned = attendance.HomeworkAssigned,
                nextLessonPrep = attendance.NextLessonPrep,
                studentPerformanceScore = attendance.StudentPerformanceScore
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(AttendanceViewModel model)
        {
            try
            {
                // Check if attendance already exists for this schedule
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.ClassScheduleId == model.ClassScheduleId);
                if (existingAttendance != null)
                {
                    return Json(new { success = false, message = "Attendance already recorded for this class schedule." });
                }

                // Get the class schedule
                var classSchedule = await _context.ClassSchedules.FindAsync(model.ClassScheduleId);
                if (classSchedule == null)
                {
                    return Json(new { success = false, message = "Class schedule not found." });
                }

                var attendance = new Attendance
                {
                    ClassScheduleId = model.ClassScheduleId,
                    StudentId = model.StudentId,
                    TeacherId = model.TeacherId,
                    AttendanceDate = DateOnly.FromDateTime(model.AttendanceDate),
                    ActualStartTime = model.ActualStartTime.HasValue ? TimeOnly.FromTimeSpan(model.ActualStartTime.Value) : null,
                    ActualEndTime = model.ActualEndTime.HasValue ? TimeOnly.FromTimeSpan(model.ActualEndTime.Value) : null,
                    Status = model.Status,
                    LessonTopic = model.LessonTopic,
                    StudentProgress = model.StudentProgress,
                    TeacherNotes = model.TeacherNotes,
                    HomeworkAssigned = model.HomeworkAssigned,
                    NextLessonPrep = model.NextLessonPrep,
                    StudentPerformanceScore = model.StudentPerformanceScore,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.Attendances.Add(attendance);

                // Update class schedule status to Completed if student was present
                if (model.Status == "Present")
                {
                    classSchedule.Status = "Completed";
                    classSchedule.UpdatedBy = GetCurrentUserId();
                    classSchedule.UpdatedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Attendance recorded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error recording attendance: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(AttendanceViewModel model)
        {
            try
            {
                var attendance = await _context.Attendances
                    .Include(a => a.ClassSchedule)
                    .FirstOrDefaultAsync(a => a.AttendanceId == model.AttendanceId);
                if (attendance == null)
                {
                    return Json(new { success = false, message = "Attendance record not found." });
                }

                attendance.AttendanceDate = DateOnly.FromDateTime(model.AttendanceDate);
                attendance.ActualStartTime = model.ActualStartTime.HasValue ? TimeOnly.FromTimeSpan(model.ActualStartTime.Value) : null;
                attendance.ActualEndTime = model.ActualEndTime.HasValue ? TimeOnly.FromTimeSpan(model.ActualEndTime.Value) : null;
                attendance.Status = model.Status;
                attendance.LessonTopic = model.LessonTopic;
                attendance.StudentProgress = model.StudentProgress;
                attendance.TeacherNotes = model.TeacherNotes;
                attendance.HomeworkAssigned = model.HomeworkAssigned;
                attendance.NextLessonPrep = model.NextLessonPrep;
                attendance.StudentPerformanceScore = model.StudentPerformanceScore;
                attendance.UpdatedBy = GetCurrentUserId();
                attendance.UpdatedDate = DateTime.Now;

                // Update class schedule status based on attendance status
                if (attendance.ClassSchedule != null)
                {
                    if (model.Status == "Present")
                    {
                        attendance.ClassSchedule.Status = "Completed";
                    }
                    else if (model.Status == "Absent")
                    {
                        attendance.ClassSchedule.Status = "Missed";
                    }
                    attendance.ClassSchedule.UpdatedBy = GetCurrentUserId();
                    attendance.ClassSchedule.UpdatedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Attendance updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating attendance: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var attendance = await _context.Attendances
                    .Include(a => a.ClassSchedule)
                    .FirstOrDefaultAsync(a => a.AttendanceId == id);
                if (attendance == null)
                {
                    return Json(new { success = false, message = "Attendance record not found." });
                }

                // Revert class schedule status back to Scheduled
                if (attendance.ClassSchedule != null)
                {
                    attendance.ClassSchedule.Status = "Scheduled";
                    attendance.ClassSchedule.UpdatedBy = GetCurrentUserId();
                    attendance.ClassSchedule.UpdatedDate = DateTime.Now;
                }

                // Hard delete attendance record
                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Attendance record deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting attendance: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetScheduleDetails(int scheduleId)
        {
            var schedule = await _context.ClassSchedules
                .Include(cs => cs.Student)
                .Include(cs => cs.Teacher)
                .ThenInclude(t => t.User)
                .Include(cs => cs.Grade)
                .FirstOrDefaultAsync(cs => cs.ClassScheduleId == scheduleId);

            if (schedule == null)
                return NotFound();

            return Json(new
            {
                classScheduleId = schedule.ClassScheduleId,
                studentId = schedule.StudentId,
                studentName = $"{schedule.Student?.FirstName} {schedule.Student?.LastName}",
                teacherId = schedule.TeacherId,
                teacherName = $"{schedule.Teacher?.User?.FirstName} {schedule.Teacher?.User?.LastName}",
                scheduleDate = schedule.ScheduleDate.ToString("yyyy-MM-dd"),
                startTime = schedule.StartTime.ToString(@"hh\:mm"),
                endTime = schedule.EndTime.ToString(@"hh\:mm"),
                gradeName = schedule.Grade?.GradeName
            });
        }

        [HttpPost]
        public async Task<IActionResult> QuickAttendance(int scheduleId, string status)
        {
            try
            {
                // Check if attendance already exists
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.ClassScheduleId == scheduleId);
                if (existingAttendance != null)
                {
                    return Json(new { success = false, message = "Attendance already recorded for this class." });
                }

                // Get schedule details
                var schedule = await _context.ClassSchedules
                    .FirstOrDefaultAsync(cs => cs.ClassScheduleId == scheduleId);
                if (schedule == null)
                {
                    return Json(new { success = false, message = "Schedule not found." });
                }

                var attendance = new Attendance
                {
                    ClassScheduleId = scheduleId,
                    StudentId = schedule.StudentId,
                    TeacherId = schedule.TeacherId,
                    AttendanceDate = schedule.ScheduleDate,
                    ActualStartTime = status == "Present" ? schedule.StartTime : null,
                    ActualEndTime = status == "Present" ? schedule.EndTime : null,
                    Status = status,
                    LessonTopic = null,
                    StudentProgress = null,
                    TeacherNotes = null,
                    HomeworkAssigned = null,
                    NextLessonPrep = null,
                    StudentPerformanceScore = null,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.Attendances.Add(attendance);

                // Update schedule status
                schedule.Status = status == "Present" ? "Completed" : "Missed";
                schedule.UpdatedBy = GetCurrentUserId();
                schedule.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Quick attendance marked as {status}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error recording quick attendance: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceReport(int studentId, DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.Today.AddMonths(-1);
            endDate ??= DateTime.Today;

            var attendances = await _context.Attendances
                .Include(a => a.ClassSchedule)
                .Where(a => a.StudentId == studentId
                    && a.AttendanceDate >= DateOnly.FromDateTime(startDate.Value)
                    && a.AttendanceDate <= DateOnly.FromDateTime(endDate.Value))
                .OrderBy(a => a.AttendanceDate)
                .Select(a => new {
                    Date = a.AttendanceDate.ToString("yyyy-MM-dd"),
                    Status = a.Status,
                    StartTime = a.ActualStartTime != null ? a.ActualStartTime.Value.ToString(@"hh\:mm") : "",
                    EndTime = a.ActualEndTime != null ? a.ActualEndTime.Value.ToString(@"hh\:mm") : "",
                    Topic = a.LessonTopic ?? "",
                    Score = a.StudentPerformanceScore ?? 0
                })
                .ToListAsync();

            var totalClasses = attendances.Count();
            var presentCount = attendances.Count(a => a.Status == "Present");
            var absentCount = attendances.Count(a => a.Status == "Absent");
            var lateCount = attendances.Count(a => a.Status == "Late");

            return Json(new
            {
                attendances = attendances,
                summary = new
                {
                    totalClasses = totalClasses,
                    presentCount = presentCount,
                    absentCount = absentCount,
                    lateCount = lateCount,
                    attendanceRate = totalClasses > 0 ? Math.Round((double)presentCount / totalClasses * 100, 1) : 0
                }
            });
        }
    }
}

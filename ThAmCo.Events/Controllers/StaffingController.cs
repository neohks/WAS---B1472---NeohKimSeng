using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThAmCo.Events.Data;

namespace ThAmCo.Events.Controllers
{
    public class StaffingController : Controller
    {
        private readonly EventsDbContext _eventContext;

        public StaffingController(EventsDbContext eventContext)
        {
            _eventContext = eventContext;
        }

        // GET: All Staffings
        public async Task<IActionResult> StaffingIndex()
        {
            var staffDbContext = _eventContext.Staffing.Include(s => s.Staff).Include(e => e.Event);
            var eventDbContext = _eventContext.Events;
            var guestDbContext = _eventContext.Guests;

            var indexVm = new ViewModels.Staffing.Index(
                await staffDbContext.ToListAsync(),
                await eventDbContext.ToListAsync(),
                await guestDbContext.ToListAsync(),
                0,
                0,
                "",
                "");

            return View("StaffingIndex", indexVm);
        }

        // GET: Staffing Create Page
        public IActionResult Create()
        {
            ViewData["StaffId"] = new SelectList(_eventContext.Staff, "StaffId", "Fullname");
            ViewData["EventId"] = new SelectList(_eventContext.Events, "Id", "Title");

            return View();
        }

        // GET: From Event to StaffBooking
        public IActionResult CreateStaffEvent(int eventid)
        {
            ViewData["StaffId"] = new SelectList(_eventContext.Staff, "StaffId", "Fullname");
            ViewData["EventId"] = new SelectList(_eventContext.Events, "Id", "Title", eventid);
            var @event = _eventContext.Events.FirstOrDefault(a=>a.Id == eventid);

            Staffing staff = new Staffing()
            {
                Event = @event,
                EventId = @event.Id,
            };

            return View(staff);
        }

        // POST: Create Guest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StaffId,Staff,EventId,Event")] Staffing staff)
        {

            var selectedStaff = _eventContext.Staffing
                .Include(a => a.Event)
                .Include(a => a.Staff)
                .FirstOrDefault(a => a.StaffId == staff.StaffId && a.EventId == staff.EventId);

            if (ModelState.IsValid)
            {
                try
                {
                    _eventContext.Add(staff);
                    await _eventContext.SaveChangesAsync();
                    return RedirectToAction(nameof(StaffingIndex));
                }
                catch (InvalidOperationException)
                {
                    TempData["msg"] = $"Error : {selectedStaff.Staff.Fullname} and {selectedStaff.Event.Title} already existed!";
                }
                catch (Exception ex)
                {
                    TempData["msg"] = ex.Message;
                }
            }
            else
            {
                TempData["msg"] = "ModelState is not valid, please verify guestbooking model";
            }

            ViewData["StaffId"] = new SelectList(_eventContext.Staff, "StaffId", "Fullname", staff.Staff);
            ViewData["EventId"] = new SelectList(_eventContext.Events, "Id", "Title", staff.Event);
            return View(staff);
        }

        #region DELETE
        // GET: Staffing Delete Page
        public async Task<IActionResult> Delete(int? staffid, int? eventid)
        {
            if (staffid == null || eventid == null)
            {
                return NotFound();
            }

            var staff = await _eventContext.Staffing
                .Include(g => g.Staff)
                .Include(g => g.Event)
                .FirstOrDefaultAsync(m => m.StaffId == staffid && m.EventId == eventid);

            if (staff == null)
            {
                return NotFound();
            }

            return View(staff);
        }

        //POST: Staffing Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Dictionary<string, int> pairIDs, [Bind("StaffId,EventId")] Staffing staff)
        {
            int staffid = pairIDs["StaffId"];
            int eventid = pairIDs["EventId"];

            staff = await _eventContext.Staffing
                .Include(g => g.Staff)
                .Include(g => g.Event)
                .FirstOrDefaultAsync(m => m.EventId == eventid && m.StaffId == staffid);

            try
            {
                _eventContext.Staffing.Remove(staff);
                await _eventContext.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
            return RedirectToAction(nameof(StaffingIndex));
        }


    }
    #endregion
}
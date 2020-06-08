using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThAmCo.Events.Data;

namespace ThAmCo.Events.Controllers
{
    public class StaffController : Controller
    {
        private readonly EventsDbContext _eventContext;
        private string anonName = "null";
        private string anonMail = "null@null.null";

        public StaffController(EventsDbContext eventContext)
        {
            _eventContext = eventContext;
        }

        // GET : All Staffs
        public async Task<IActionResult> StaffIndex()
        {
            return View(await _eventContext.Staff.ToListAsync());
        }

        // GET : Staff Details page
        public async Task<IActionResult> Details(int? staffid)
        {
            if (staffid == null)
                return NotFound();

            var staff = await _eventContext.Staff
                .Include(s => s.Staffing)
                .ThenInclude(e => e.Event)
                .FirstOrDefaultAsync(a => a.StaffId == staffid);

            if (staff == null)
                return NotFound();

            return View(staff);

        }

        // GET: Staff Create page
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create a staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Surname,FirstName,Email,FirstAider")] Staff staff)
        {
            if (ModelState.IsValid)
            {
                _eventContext.Add(staff);
                await _eventContext.SaveChangesAsync();
                return RedirectToAction(nameof(StaffIndex));
            }
            return View(staff);
        }

        // GET: Staff Edit page
        public async Task<IActionResult> Edit(int? staffid)
        {
            if (staffid == null)
            {
                return NotFound();
            }

            var staff = await _eventContext.Staff.FindAsync(staffid);
            if (staff == null)
            {
                return NotFound();
            }
            return View(staff);
        }

        // POST: Edit a staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int staffid, [Bind("StaffId,Surname,FirstName,Email,FirstAider")] Staff staff)
        {
            if (staffid != staff.StaffId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _eventContext.Update(staff);
                    await _eventContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_eventContext.Staff.Any(e => e.StaffId == staffid))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(StaffIndex));
            }
            return View(staff);
        }

        // GET: Staff Delete page
        public async Task<IActionResult> Delete(int? staffid)
        {
            if (staffid == null)
                return NotFound();

            var staff = await _eventContext.Staff
                .FirstOrDefaultAsync(m => m.StaffId == staffid);

            if (staff.FirstName == anonName && staff.Surname == anonName && staff.Email == anonMail)
                TempData["Anonymise"] = "Done";

            if (staff == null)
                return NotFound();

            return View(staff);
        }

        // POST: Delete a staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int staffid)
        {
            var staff = await _eventContext.Staff.FindAsync(staffid);
            _eventContext.Staff.Remove(staff);

            await _eventContext.SaveChangesAsync();
            return RedirectToAction(nameof(StaffIndex));
        }

        // GET: Permenantly Anonymise a Staff
        public async Task<IActionResult> Anonymise(int staffid)
        {
            var staff = await _eventContext.Staff
                .FirstOrDefaultAsync(m => m.StaffId == staffid);
            if (staff == null)
            {
                return NotFound();
            }
            staff.FirstName = anonName;
            staff.Surname = anonName;
            staff.Email = anonMail;
            //Tell View to enable Delete button
            TempData["Anonymise"] = "Done";

            _eventContext.Update(staff);
            await _eventContext.SaveChangesAsync();

            return RedirectToAction("Delete", "Staff", new { staffid });
        }

    }
}
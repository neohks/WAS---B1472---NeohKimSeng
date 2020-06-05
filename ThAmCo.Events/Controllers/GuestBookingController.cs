﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThAmCo.Events.Data;

namespace ThAmCo.Events.Controllers
{
    public class GuestBookingController : Controller
    {
        private readonly EventsDbContext _eventDB;

        public GuestBookingController(EventsDbContext eventDB)
        {
            _eventDB = eventDB;
        }

        #region Indexes

        //GET: All Guests   
        public async Task<IActionResult> GuestIndex()
        {
            var guestDbContext = _eventDB.Guests.Include(t => t.Customer).Include(t => t.Event);

            var indexVm = new ViewModels.Guests.Index(
                await guestDbContext.ToListAsync(),
                0,
                0,
                "",
                "");

            return View("GuestIndex", indexVm);
        }

        // GET: GuestBooking (filtered by CusId)
        public async Task<IActionResult> CustomerFilteredIndex(int id, string cusName)
        {
            var guestDbContext = _eventDB.Guests.Include(t => t.Customer).Include(t => t.Event).Where(t => t.CustomerId == id);


            var indexVm = new ViewModels.Guests.Index(
                await guestDbContext.ToListAsync(),
                id,
                0,
                cusName,
                "");

            return View("CustomerFilteredIndex", indexVm);
        }

        // GET: GuestBooking (filtered by EventId)
        public async Task<IActionResult> EventFilteredIndex(int id, string title)
        {
            var guestDbContext = _eventDB.Guests.Include(t => t.Customer).Include(t => t.Event).Where(t => t.EventId == id);

            var indexVm = new ViewModels.Guests.Index(
                await guestDbContext.ToListAsync(),
                0,
                id,
                "",
                title);

            return View("EventFilteredIndex", indexVm);
        }

        #endregion

        #region Create

        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(_eventDB.Customers, "Id", "Fullname");
            ViewData["EventId"] = new SelectList(_eventDB.Events, "Id", "Title");

            return View();
        }

        // POST: Create Guest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerId,Customer,EventId,Event,Attended")] GuestBooking guest)
        {
            if (ModelState.IsValid)
            {
                _eventDB.Add(guest);

                try
                {
                    await _eventDB.SaveChangesAsync();
                }
                catch (Exception)
                {
                    TempData["msg"] = "<script>alert('Change succesfully');</script>";
                    
                    throw;
                }

                return RedirectToAction(nameof(GuestIndex));
            }
            ViewData["CustomerId"] = new SelectList(_eventDB.Customers, "Id", "Fullname", guest.Customer);
            ViewData["EventId"] = new SelectList(_eventDB.Events, "Id", "Title", guest.Event);
            return View(guest);
        }

        // GET: Guests /Create (Filtered by customer)
        public IActionResult CustomerFilteredCreate(int? cusId)
        {
            ViewData["CustomerId"] = new SelectList(_eventDB.Customers, "Id", "Fullname");
            ViewData["EventId"] = new SelectList(_eventDB.Events, "Id", "Title");

            var guest = new GuestBooking()
            {
                CustomerId = cusId.HasValue ? cusId.Value : 0
            };

            return View("Create");
        }

        // GET: Timesheet/Create (Filtered by event)
        public IActionResult EventFilteredCreate(int? eventId)
        {
            ViewData["CustomerId"] = new SelectList(_eventDB.Customers, "Id", "Fullname");
            ViewData["EventId"] = new SelectList(_eventDB.Events, "Id", "Title");

            var guest = new GuestBooking()
            {
                EventId = eventId.HasValue ? eventId.Value : 0
            };


            return View("Create");
        }

        #endregion

        #region Edit
        //Issue: Use Dictionary cannot solve as Attend is Bool, not Int.
        private static int? editCusId; //Workaround for Post Edit
        // GET: GuestBooking Edit Page
        public async Task<IActionResult> Edit(int? cusid, int? eventid)
        {
            if (cusid == null || eventid == null)
            {
                return NotFound();
            }
            editCusId = cusid;

            var guest = await _eventDB.Guests.FindAsync(cusid, eventid);
            if (guest == null)
            {
                return NotFound();
            }
            ViewData["CustomerId"] = new SelectList(_eventDB.Customers, "Id", "Fullname", guest.CustomerId);
            ViewData["EventId"] = new SelectList(_eventDB.Events, "Id", "Title", guest.EventId);
            return View(guest);
        }

        //POST: GuestBooking Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int cusid, int eventid, [Bind("CustomerId,Customer,EventId,Event,Attended")] GuestBooking guest)
        {
            //int cusid = pairIDs["CustomerId"];
            //int eventid = pairIDs["EventId"];

            if (eventid != guest.EventId || editCusId != guest.CustomerId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _eventDB.Update(guest);
                    await _eventDB.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_eventDB.Guests.Any(e => e.CustomerId == guest.CustomerId && e.EventId == guest.EventId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(GuestIndex));
            }
            ViewData["CustomerId"] = new SelectList(_eventDB.Customers, "Id", "Fullname", guest.CustomerId);
            ViewData["EventId"] = new SelectList(_eventDB.Events, "Id", "Title", guest.EventId);
            return View(guest);
        }

        #endregion

        #region Delete

        // GET: GuestBooking Delete Page
        public async Task<IActionResult> Delete(int? cusid, int? eventid)
        {
            if (cusid == null || eventid == null)
            {
                return NotFound();
            }

            var guest = await _eventDB.Guests
                .Include(g => g.Customer)
                .Include(g => g.Event)
                .FirstOrDefaultAsync(m => m.CustomerId == cusid && m.EventId == eventid);

            if (guest == null)
            {
                return NotFound();
            }

            return View(guest);
        }

        //POST: GuestBooking Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Dictionary<string, int> pairIDs, [Bind("CustomerId,EventId")] GuestBooking guest)
        {
            int cusid = pairIDs["CustomerId"];
            int eventid = pairIDs["EventId"];

            guest = await _eventDB.Guests
                .Include(g => g.Customer)
                .Include(g => g.Event)
                .FirstOrDefaultAsync(m => m.EventId == eventid && m.CustomerId == cusid);

            try
            {
                _eventDB.Guests.Remove(guest);
                await _eventDB.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
            return RedirectToAction(nameof(GuestIndex));
        }

        #endregion
    }
}
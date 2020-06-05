using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThAmCo.Events.Data;

namespace ThAmCo.Events.Controllers
{
    public class EventController : Controller
    {
        private readonly EventsDbContext _eventDB;
        [BindProperty]
        public Event Event { get; set; }

        public EventController(EventsDbContext eventDB)
        {
            _eventDB = eventDB;
        }

        //GET: All Events
        public async Task<IActionResult> EventIndex()
        {
            return View(await _eventDB.Events.ToListAsync());
        }

        // GET: Event Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _eventDB.Events
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // Goto: Event Create page
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create an Event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Date,Duration,TypeId")] Event @event)
        {
            if (ModelState.IsValid)
            {
                _eventDB.Add(@event);
                await _eventDB.SaveChangesAsync();
                return RedirectToAction(nameof(EventIndex)); //Go back to Index (Event Main Page)
            }
            return View(@event);
        }

        // GET: Event Edit page
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _eventDB.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            return View(@event);
        }

        // POST: Edit an Event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Date,Duration,TypeId")] Event @event)
        {
            if (id != @event.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _eventDB.Update(@event);
                    await _eventDB.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_eventDB.Events.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(EventIndex));
            }
            return View(@event);
        }


    }
}
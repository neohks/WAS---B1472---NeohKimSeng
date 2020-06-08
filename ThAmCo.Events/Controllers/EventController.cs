using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThAmCo.Events.Data;
using ThAmCo.Events.Services;
using ThAmCo.Events.ViewModels;

namespace ThAmCo.Events.Controllers
{
    public class EventController : Controller
    {
        private readonly EventsDbContext _eventContext;
        [BindProperty]
        public Event Event { get; set; }

        public EventController(EventsDbContext eventContext)
        {
            _eventContext = eventContext;
        }


        // GET: All Events
        public async Task<IActionResult> EventIndex()
        {
            //Should add API call to check Venue
            return View(await _eventContext.Events.Include(b => b.Bookings).Include(s=> s.Staffings).ThenInclude(s => s.Staff).ToListAsync());
        }

        // GET: Event Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _eventContext.Events
                .Include(b => b.Bookings)
                .ThenInclude(g => g.Customer)
                .Include(a => a.Staffings)
                .ThenInclude(g => g.Staff)
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
                _eventContext.Add(@event);
                await _eventContext.SaveChangesAsync();
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

            var @event = await _eventContext.Events.FindAsync(id);
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
                    _eventContext.Update(@event);
                    await _eventContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_eventContext.Events.Any(e => e.Id == id))
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

        // Get: Reserve a Venue for Event
        public async Task<IActionResult> ReserveVenue(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            //Get the details of selected event based on its ID
            var @event = await _eventContext.Events
                .FirstOrDefaultAsync(m => m.Id == id);

            HttpClient client = getClient("23652");

            //Make a query for availibility request
            //AvailabilityController need 3 params
            //Compile them into one string, then get async from web api
            String url = $"api/Availability?" +
                $"eventType={@event.TypeId}&" +
                $"beginDate={@event.Date.ToString("yyyy/MM/dd HH:mm:ss")}&" +
                $"endDate={@event.Date.Add(@event.Duration.Value).ToString("yyyy/MM/dd HH:mm:ss")}";

            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)  
            {
                var venue = await response.Content.ReadAsAsync<IEnumerable<VenueEvent>>();

                if (venue.Count() == 0)
                {
                    ViewData["None"] = "None";
                }
                else
                {
                    ViewData["Venues"] = new SelectList(venue, "Code", "Name");
                }
            }
            else
            {
                //Show alert message?
                ModelState.AddModelError("Error", "Connection has failed, please try again.");
            }

            return View();

        }

        // POST: Reserve an event
        public async Task<IActionResult> ConfirmReservation(int id, string VenueCode, int staffId)
        {
            var @event = await _eventContext.Events
               .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            HttpClient client = getClient("23652");

            //If alrdy have venue reserve, then delete the old one
            if (@event.VenueCode != null)
            {
                
                //Build the url api, delete the reserved venue
                String url = "api/Reservations?" + @event.VenueCode;

                HttpResponseMessage responseRes = await client.DeleteAsync(url);

                if (responseRes.IsSuccessStatusCode)
                {
                    @event.VenueCode = null;
                    _eventContext.Update(@event);
                    await _eventContext.SaveChangesAsync();
                }
                else
                {
                    //Prompt Error Msg
                }
            }

            ReservationPostApi req = new ReservationPostApi
            {
                EventDate = @event.Date,
                VenueCode = VenueCode,
                StaffId = staffId //Null for now, later if can, add this
            };

            HttpResponseMessage response = await client.PostAsJsonAsync("api/Reservations", req);

            if (response.IsSuccessStatusCode)
            {
                var Reservation = await response.Content.ReadAsAsync<ReservationGetApi>();

                @event.VenueCode = Reservation.VenueCode;
                @event.VenueReference = Reservation.Reference;
                _eventContext.Update(@event);

                await _eventContext.SaveChangesAsync();
            }

            return RedirectToAction(nameof(EventIndex));
        }

        // Cancel (Soft delete) Venue and Staffs
        public async Task<IActionResult> CancelEvent(int id)
        {
            var @event = await _eventContext.Events.Include(e => e.Staffings).FirstOrDefaultAsync(m => m.Id == id);

            HttpClient client = getClient("23652");
            //If have Venue, delete it first
            if (@event.VenueReference != null)
            {
                string url = "api/Reservations/" + @event.VenueReference;

                HttpResponseMessage responseRes = await client.DeleteAsync(url);

                if (responseRes.IsSuccessStatusCode)
                {
                    @event.VenueReference = null;
                    @event.VenueCode = null;

                    _eventContext.Update(@event);
                    await _eventContext.SaveChangesAsync();
                }
            }

            if (@event.Staffings.Count() > 0)
            {
                foreach (Staffing s in @event.Staffings)
                {
                    _eventContext.Remove(s);
                }

                await _eventContext.SaveChangesAsync();
            }

            return RedirectToAction(nameof(EventIndex));
        }



        // Connects an HttpClient to a selected port
        private HttpClient getClient(string port)
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:" + port)
            };
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            return client;
        }

    }
}
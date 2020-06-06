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
            return View(await _eventContext.Events.Include(sb => sb.Bookings).ToListAsync());
        }

        // GET: Event Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _eventContext.Events
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

            //Check for the availability first
            HttpClient client = new HttpClient();
            var VenueBuilder = new UriBuilder("http://localhost");
            VenueBuilder.Port = 23652;
            VenueBuilder.Path = "api/Availability";

            //Make a query for availibility request
            //AvailabilityController need 3 params
            //api/Availability?eventType=X?beginDate=X&endDate=X
            var VenueQuery = HttpUtility.ParseQueryString(VenueBuilder.Query);
            VenueQuery["eventType"] = @event.TypeId;
            VenueQuery["beginDate"] = @event.Date.ToString("yyyy/MM/dd HH:mm:ss");
            VenueQuery["endDate"] = @event.Date.Add(@event.Duration.Value).ToString("yyyy/MM/dd HH:mm:ss");
            VenueBuilder.Query = VenueQuery.ToString();

            //Compile them into one string, then get async from web api
            String url = VenueBuilder.ToString();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            HttpResponseMessage response = await client.GetAsync(url);

            var Venue = await response.Content.ReadAsAsync<IEnumerable<VenueEvent>>();
            if (response.IsSuccessStatusCode)
            {
                if (Venue.Count() == 0)
                {
                    ViewData["None"] = "None";
                }
                else
                {
                    ViewData["Venues"] = new SelectList(Venue, "Code", "Name");
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
        public async Task<IActionResult> ConfirmReservation(int id, string VenueCode, int StaffId)
        {
            var @event = await _eventContext.Events
               .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            //If alrdy have venue reserve, then delete the old one
            if (@event.VenueCode != null)
            {
                HttpClient client1 = new HttpClient();

                //Build the url api
                var VenueBuilder = new UriBuilder("http://localhost");
                VenueBuilder.Port = 23652;
                VenueBuilder.Path = "api/Reservations/" + @event.VenueCode;
                client1.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                string url = VenueBuilder.ToString();


                HttpResponseMessage response1 = await client1.DeleteAsync(url);

                if (response1.IsSuccessStatusCode)
                {

                    @event.VenueCode = null;
                    _eventContext.Update(@event);
                    await _eventContext.SaveChangesAsync();
                }
            }

            HttpClient client = new HttpClient();
            client.BaseAddress = new System.Uri("http://localhost:23652");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            ReservationPostApi req = new ReservationPostApi
            {
                EventDate = @event.Date,
                VenueCode = VenueCode,
                StaffId = StaffId
            };

            HttpResponseMessage response = await client.PostAsJsonAsync("api/Reservations", req);

            if (response.IsSuccessStatusCode)
            {
                var Reservation = await response.Content.ReadAsAsync<ReservationGetApi>();

                @event.VenueCode = Reservation.Reference;
                _eventContext.Update(@event);

                await _eventContext.SaveChangesAsync();
            }

            return RedirectToAction(nameof(EventIndex));
        }



    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Rewrite.Internal.UrlActions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ThAmCo.Events.Data;
using ThAmCo.Events.Services;
using ThAmCo.Events.ViewModels.Venues;
using ThAmCo.Venues.Data;

namespace ThAmCo.Events.Controllers
{
    public class EventController : Controller
    {
        private readonly EventsDbContext _eventContext;

        public EventController(EventsDbContext eventContext)
        {
            // Inital call HttpClient
            var client = getEventTypes();
      
            

            _eventContext = eventContext;
        }


        // GET: All Events
        public async Task<IActionResult> EventIndex()
        {
            var events = await _eventContext.Events
                .Include(b => b.Bookings)
                .Include(s => s.Staffings)
                .ThenInclude(s => s.Staff)
                .ToListAsync();

            foreach (var item in events)
            {
                item.VenueCode = item.VenueCode ?? "None";
            }

            return View(events);
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

            // Get Event Type name
            HttpClient client = getClient("23652");
            var eventTypesResponse = await client.GetAsync("api/EventTypes");
            var eventTypes = await eventTypesResponse.Content.ReadAsAsync<IEnumerable<EventType>>();
            var selectedEvent  = eventTypes.FirstOrDefault(a => a.Id == @event.TypeId);
            ViewData["EventType"] = selectedEvent.Title;

            // If VenueCode exists
            // Then find its full name through API
            if (!string.IsNullOrEmpty(@event.VenueCode))
            {
                var venues = await getVenuesAsync(selectedEvent.Id, DateTime.MinValue, DateTime.MaxValue);

                var venue = venues.FirstOrDefault(a => a.Code == @event.VenueCode);

                @event.VenueCode = venue.Name;
            }

            //Set None if null
            @event.VenueCode = @event.VenueCode ?? "None";
            @event.VenueReference = @event.VenueReference ?? "None";

            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // Goto: Event Create page
        public async Task<IActionResult> Create()
        {
            var eventTypes = await getEventTypes();
            ViewData["EventType"] = new SelectList(eventTypes, "Id", "Title");

            return View();
        }

        // POST: Create an Event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Date,Duration,TypeId")] Event @event)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _eventContext.Add(@event);
                    await _eventContext.SaveChangesAsync();
                    return RedirectToAction(nameof(EventIndex));
                }
                catch (OverflowException)
                {
                    TempData["msg"] = $"Please type '{@event.Duration}:XX' instead of just '{@event.Duration}' alone";
                }
                catch (DbUpdateException)
                {
                    TempData["msg"] = "There is an error while creating, please ensure your textfield are in correct format:";
                    TempData["msgDuration"] = $"Please ensure '{@event.Duration}' in HH:mm format instead of just '{@event.Duration}'";
                    TempData["msgDate"] = $"Please ensure '{@event.Date.ToString("dd/MM/yyyy")}' in dd/MM/yyyy format instead of '{@event.Date}'";
                }
                catch (Exception ex)
                {
                    TempData["msg"] = ex.Message;
                }
                
            }
            else
            {
                TempData["msg"] = "There is an error while with the model state, contact admin asap";
            }

            var eventTypes = await getEventTypes();
            ViewData["EventType"] = new SelectList(eventTypes, "Id", "Title");

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

        private SelectList viewVenueList, viewVenues, viewStaffs;

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

            if (@event == null)
            {
                return NotFound();
            }

            HttpClient client = getClient("23652");

            // Make a query for availibility request
            // AvailabilityController need 3 params
            String url = $"api/Availability?" +
                $"eventType={@event.TypeId}&" +
                $"beginDate={@event.Date.ToString("yyyy/MM/dd HH:mm:ss")}&" +
                $"endDate={@event.Date.Add(@event.Duration.Value).ToString("yyyy/MM/dd HH:mm:ss")}";
            // Compile them into one string, then get async from web api
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var venue = await response.Content.ReadAsAsync<IEnumerable<VenueEvent>>();

                if (venue.Count() == 0)
                {
                    ViewData["None"] = "None";
                    TempData["None"] = "No Staff Available";
                }
                else
                {
                    var staffs = _eventContext.Staff;

                    ViewData["VenueList"] = new SelectList(venue.ToList());
                    viewVenueList = new SelectList(venue.ToList());

                    ViewData["Venues"] = new SelectList(venue, "Code", "Name");
                    viewVenues = new SelectList(venue, "Code", "Name");

                    ViewData["Staffs"] = new SelectList(staffs, "StaffId", "Fullname");
                    viewStaffs = new SelectList(staffs, "StaffId", "Fullname");
                }
            }
            else
            {
                // TODO : Show alert message?
                ModelState.AddModelError("Error", "Connection has failed, please try again.");
            }

            return View(@event);

        }

        // POST: Reserve an event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReservation(int id, string VenueCode, int Staffings)
        {
            var @event = await _eventContext.Events
               .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            HttpClient client = getClient("23652");

            //If alrdy have venue reserve, then delete the old one
            if (@event.VenueReference != null)
            {
                //Build the url api, delete the reserved venue
                String urlRes = "api/Reservations/" + @event.VenueReference;

                HttpResponseMessage responseRes = await client.DeleteAsync(urlRes);

                if (responseRes.IsSuccessStatusCode)
                {
                    @event.VenueReference = null;
                    _eventContext.Update(@event);
                    await _eventContext.SaveChangesAsync();
                }
                else
                {
                    // TODO : Prompt Error Msg
                }
            }

            //Set the Venue Cost
            var venues = await getVenuesAsync(@event.TypeId, @event.Date, @event.Date.Add(@event.Duration.Value));
            var vCost = venues.FirstOrDefault(a => a.Code == VenueCode);

            @event.VenueCost = vCost.CostPerHour;

            ReservationPostApi req = new ReservationPostApi
            {
                EventDate = @event.Date,
                VenueCode = VenueCode,
                StaffId = Staffings
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

            //Get Cost
            

            return RedirectToAction(nameof(EventIndex));
        }

        // GET: Event Cancel/Delete Page
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _eventContext.Events
                .Include(a => a.Staffings)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            // Check whether it soft deleted or not
            if (@event.Staffings.Count() == 0 && string.IsNullOrEmpty(@event.VenueCode) 
                && string.IsNullOrEmpty(@event.VenueReference) && @event.VenueCost == 0)
                TempData["Cancel"] = "Done";

           
            return View(@event);
        }

        // POST: Delete a Customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var @event = await _eventContext.Events.FindAsync(id);
            _eventContext.Remove(@event);

            await _eventContext.SaveChangesAsync();
            return RedirectToAction(nameof(EventIndex));
        }

        // POST: Cancel (Soft delete) Venue and Staffs
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
                    @event.VenueCost = 0;

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

            TempData["Cancel"] = "Done";

            return RedirectToAction("Cancel", "Event", new { id });
        }


        private async Task<IEnumerable<EventType>> getEventTypes()
        {
            try
            {
                HttpClient client = getClient("23652");
                var eventTypesResponse = await client.GetAsync("api/EventTypes");
                var eventTypes = await eventTypesResponse.Content.ReadAsAsync<IEnumerable<EventType>>();
                return eventTypes;
            }
            catch (Exception)
            {
                //Ask user to ask admin to connect to server
                throw;
            }
        }

        private async Task<IEnumerable<VenueAvailibilityGetApi>> getVenuesAsync(string eventType, DateTime startDate, DateTime endDate)
        {
            var availableVenues = new List<VenueAvailibilityGetApi>().AsEnumerable();

            HttpClient client = getClient("23652");

            String url = $"api/Availability?" +
                $"eventType={eventType}&" +
                $"beginDate={startDate.ToString("yyyy/MM/dd")}&" +
                $"endDate={endDate.ToString("yyyy/MM/dd")}";

            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                availableVenues = await response.Content.ReadAsAsync<IEnumerable<VenueAvailibilityGetApi>>();

                if (availableVenues.Count() == 0)
                {
                    //No received
                    TempData["Empty"] = "No Data Found";
                }
            }


            return availableVenues.ToList().GroupBy(o => o.Name).Select(o => o.FirstOrDefault());

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
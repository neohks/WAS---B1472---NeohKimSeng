using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using ThAmCo.Events.Data;
using ThAmCo.Events.Services;
using ThAmCo.Events.ViewModels.Venues;
using ThAmCo.Venues.Data;

namespace ThAmCo.Events.Controllers
{
    public class VenueController : Controller
    {
        private readonly EventsDbContext _eventsContext;

        public VenueController(EventsDbContext eventContext)
        {
            _eventsContext = eventContext;
        }

        // GET : Availibility Venues filtered with EventType and Date range
        public async Task<IActionResult> VenueIndex(string eventType, DateTime startDate, DateTime endDate)
        {
            //If null show WED by default
            eventType = string.IsNullOrEmpty(eventType) ? "WED" : eventType;

            var availableVenues = new List<VenueAvailibilityGetApi>().AsEnumerable();

            DateTime emptyDt = new DateTime();
            startDate = (startDate == emptyDt) ? DateTime.MinValue : startDate;
            endDate = (endDate == emptyDt) ? DateTime.MaxValue : endDate;

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
            else
            {
                
                TempData["Error"] = "Fail to response";
                //Error Message
            }
            
            var eventTypesResponse = await client.GetAsync("api/EventTypes");
            var eventTypes = await eventTypesResponse.Content.ReadAsAsync<IEnumerable<EventType>>();

            ViewData["EventTypes"] = new SelectList(eventTypes.ToList(), "Id", "Title");
            ViewData["EventType"] = eventType;

            VenueVMIndex indexVM;

            if (startDate == DateTime.MinValue && endDate == DateTime.MaxValue)
            {
                indexVM = new VenueVMIndex(
                availableVenues.ToList(),
                availableVenues.ToList().GroupBy(o => o.Name).Select(o => o.FirstOrDefault()), // Group VenueName uniquely (No repeat) 
                null,
                null
                );
            }
            else
            {
                indexVM = new VenueVMIndex(
                availableVenues.ToList(),
                availableVenues.ToList().GroupBy(o => o.Name).Select(o => o.FirstOrDefault()),
                startDate,
                endDate
                );
            }

            return View("VenueIndex", indexVM);
        }

        // GET : Event Create page
        public IActionResult CreateEvent(string venueCode, string eventType, DateTime date, decimal cost)
        {
            if (venueCode == null || eventType == null)
            {
                return NotFound();
            }

            var @event = new VenueCreateModel
            {
                VenueCode = venueCode,
                TypeId = eventType,
                Date = date.ToString("dd/MM/yyyy"),
                VenueCost = cost
            };


            return View(@event);
        }

        // POST: Create an Event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent([Bind("Id,Title,TypeId,Date,Duration,VenueCode,VenueCost")] VenueCreateModel @event)
        {

            if (ModelState.IsValid)
            {
                //PostReservation
                HttpClient client = getClient("23652");

                ReservationPostApi req = new ReservationPostApi
                {
                    EventDate = DateTime.Parse(@event.Date),
                    VenueCode = @event.VenueCode,
                    StaffId = 0 //Null for now, later if can, add this
                };

                HttpResponseMessage response = await client.PostAsJsonAsync("api/Reservations", req);

                if (response.IsSuccessStatusCode)
                {
                    var Reservation = await response.Content.ReadAsAsync<ReservationGetApi>();

                    var newEvent = new Event
                    {
                        Id = @event.Id,
                        Title = @event.Title,
                        Date = DateTime.Parse(@event.Date),
                        Duration = @event.Duration,
                        TypeId = @event.TypeId,
                        VenueCode = @event.VenueCode,
                        VenueReference = Reservation.Reference,
                        VenueCost = @event.VenueCost
                    };

                    try
                    {
                        _eventsContext.Add(newEvent);
                        await _eventsContext.SaveChangesAsync();
                        return RedirectToAction("EventIndex", "Event");
                    }
                    catch (OverflowException)
                    {
                        await deleteReservationPostAsync(client, newEvent);
                        TempData["msg"] = $"Please type '{@event.Duration}:XX' instead of just '{@event.Duration}' alone";
                    }
                    catch (DbUpdateException)
                    {
                        await deleteReservationPostAsync(client, newEvent);
                        TempData["msg"] = "There is an error while creating, please ensure your textfield are in correct format:";
                        TempData["msgDuration"] = $"Please ensure '{@event.Duration}' in HH:mm format instead of just '{@event.Duration}'";
                    }
                    catch (Exception ex)
                    {
                        await deleteReservationPostAsync(client, newEvent);
                        TempData["msg"] = ex.Message;
                    }


                }
            }

            return View(@event);
        }


        //Delete Reservation if catch exception, since posted
        private async Task deleteReservationPostAsync(HttpClient client, Event eventFail)
        {

            if (eventFail.VenueReference != null)
            {
                //Build the url api, delete the reserved venue
                String urlRes = "api/Reservations/" + eventFail.VenueReference;

                HttpResponseMessage responseRes = await client.DeleteAsync(urlRes);

            }
        }

        // Connects an HttpClient to a selected port
        private HttpClient getClient(string port)
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = new System.Uri("http://localhost:" + port)
            };
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            return client;
        }

    }
}
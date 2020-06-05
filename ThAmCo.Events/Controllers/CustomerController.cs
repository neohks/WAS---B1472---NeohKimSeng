using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThAmCo.Events.Data;

namespace ThAmCo.Events.Controllers
{
    public class CustomerController : Controller
    {
        private readonly EventsDbContext _eventDB;

        public CustomerController(EventsDbContext eventdb)
        {
            _eventDB = eventdb;
        }

        //GET: All Customers
        public async Task<IActionResult> CustomerIndex()
        {
            return View(await _eventDB.Customers.ToListAsync());
        }

        // GET: Customer Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _eventDB.Customers
                .Include(b => b.Bookings)
                .ThenInclude(e => e.Event)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // Goto: Customer Create page
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create an Customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Surname,FirstName,Email")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                _eventDB.Add(customer);
                await _eventDB.SaveChangesAsync();
                return RedirectToAction(nameof(CustomerIndex)); //Go back to Index (Customer Main Page)
            }
            return View(customer);
        }

        // GET: Customer Edit page
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _eventDB.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Edit an Customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Surname,FirstName,Email")] Customer customer)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _eventDB.Update(customer);
                    await _eventDB.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_eventDB.Customers.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(CustomerIndex));
            }
            return View(customer);
        }


        // GET: Customer Delete Page
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _eventDB.Customers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Delete a Customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _eventDB.Customers.FindAsync(id);
            _eventDB.Remove(customer);

            await _eventDB.SaveChangesAsync();
            return RedirectToAction(nameof(CustomerIndex));
        }

        // POST: Permenantly Anonymise a Customer
        public async Task<IActionResult> Anonymise(int id)
        {
            var customer = await _eventDB.Customers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null)
            {
                return NotFound();
            }
            customer.FirstName = "null";
            customer.Surname = "null";
            customer.Email = "null@null.null";

            _eventDB.Update(customer);
            await _eventDB.SaveChangesAsync();
            return RedirectToAction(nameof(CustomerIndex));
        }



    }
}
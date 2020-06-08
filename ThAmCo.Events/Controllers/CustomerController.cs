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
        private readonly EventsDbContext _eventContext;
        //Anon the customer data
        private string anonName = "null";
        private string anonMail = "null@null.null";
        public CustomerController(EventsDbContext eventContext)
        {
            _eventContext = eventContext;
        }

        //GET: All Customers
        public async Task<IActionResult> CustomerIndex()
        {
            return View(await _eventContext.Customers.ToListAsync());
        }

        // GET: Customer Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _eventContext.Customers
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
                _eventContext.Add(customer);
                await _eventContext.SaveChangesAsync();
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

            var customer = await _eventContext.Customers.FindAsync(id);
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
                    _eventContext.Update(customer);
                    await _eventContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_eventContext.Customers.Any(e => e.Id == id))
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

            var customer = await _eventContext.Customers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (customer.FirstName == anonName && customer.Surname == anonName && customer.Email == anonMail)
                TempData["Anonymise"] = "Done";

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
            var customer = await _eventContext.Customers.FindAsync(id);
            _eventContext.Remove(customer);

            await _eventContext.SaveChangesAsync();
            return RedirectToAction(nameof(CustomerIndex));
        }

        // GET: Permenantly Anonymise a Customer
        public async Task<IActionResult> Anonymise(int id)
        {
            var customer = await _eventContext.Customers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null)
            {
                return NotFound();
            }
            customer.FirstName = anonName;
            customer.Surname = anonName;
            customer.Email = anonMail;
            //Tell View to enable Delete button
            TempData["Anonymise"] = "Done";

            _eventContext.Update(customer);
            await _eventContext.SaveChangesAsync();

            return RedirectToAction("Delete","Customer",new { id });
        }



    }
}
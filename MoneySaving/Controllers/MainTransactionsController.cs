﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using MoneySaving.Data;
using MoneySaving.Models;

namespace MoneySaving.Controllers
{
    [Authorize]
    public class MainTransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MainTransactionsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: MainTransactions
        public async Task<IActionResult> Index(string QueryPocket)
        {

            //var applicationDbContext = _context.MainTransaction.Include(m => m.MCategory).Include(m => m.MPocket);
            //return View(await applicationDbContext.ToListAsync());

            //var user = await _userManager.GetUserAsync(User);
            //var mainTransaction = from x in _context.MainTransaction.Include(m => m.MCategory).Include(m => m.MPocket)
            //                      where x.User == user
            //                      select x;
            //return View(mainTransaction);

            var user = await _userManager.GetUserAsync(User);

            IQueryable<string> pocketQuery = from m in _context.MPocket
                                             where m.User == user
                                             orderby m.Name
                                             select m.Name;

            var transactions = from m in _context.MainTransaction.Include(m => m.MCategory).Include(m => m.MPocket)
                               where m.User == user
                               select m;

            var pockets = from p in _context.MPocket
                          where p.User == user
                          select p;

            if (!string.IsNullOrEmpty(QueryPocket))
            {
                transactions = transactions.Where(x => x.MPocket.Name == QueryPocket);
            }

            transactions = transactions.OrderBy(m => m.TransactionDate).OrderBy(m => m.LastUpdate);

            var mainVM = new MainViewModel
            {
                PocketsSelectList = new SelectList(await pocketQuery.Distinct().ToListAsync()),
                Pockets = await pockets.ToListAsync(),
                MainTransactions = await transactions.ToListAsync()
            };

            return View(mainVM);
        }

        // GET: MainTransactions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mainTransaction = await _context.MainTransaction
                .Include(m => m.MCategory)
                .Include(m => m.MPocket)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (mainTransaction == null)
            {
                return NotFound();
            }

            return View(mainTransaction);
        }

        // GET: MainTransactions/Create
        public IActionResult Create(int CashflowTypeId)
        {
            //ViewData["MCategoryId"] = new SelectList(_context.MCategory, "ID", "Name");
            //ViewData["MpocketId"] = new SelectList(_context.MPocket, "ID", "Name");
            //return View();

            var userId = _userManager.GetUserId(User);

            ViewData["MCategoryId"] = new SelectList(_context.MCategory.Where(x => x.CashflowTypeId == CashflowTypeId && x.User.Id == userId), "ID", "Name");
            ViewData["MpocketId"] = new SelectList(_context.MPocket.Where(x => x.User.Id == userId), "ID", "Name");

            var cashflowType = _context.CashflowType.Where(x => x.ID == CashflowTypeId && x.User.Id == userId);
            if (cashflowType.ToList().Count == 1)
            {
                ViewData["Title_1"] = "Add " + cashflowType.ToList()[0].Name;
            }

            var mainTransaction = new MainTransaction();
            return View(mainTransaction);
        }

        // POST: MainTransactions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,TransactionDate,MpocketId,MCategoryId,Detail,Amount,StatusFlag,LastUpdate")] MainTransaction mainTransaction)
        {
            var user = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                mainTransaction.User = user;

                _context.Add(mainTransaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["MCategoryId"] = new SelectList(_context.MCategory.Where(x => x.User == user), "ID", "Name", mainTransaction.MCategoryId);
            ViewData["MpocketId"] = new SelectList(_context.MPocket.Where(x => x.User == user), "ID", "Name", mainTransaction.MpocketId);
            return View(mainTransaction);
        }

        // GET: MainTransactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            return NotFound();

            if (id == null)
            {
                return NotFound();
            }

            var mainTransaction = await _context.MainTransaction.FindAsync(id);
            if (mainTransaction == null)
            {
                return NotFound();
            }
            ViewData["MCategoryId"] = new SelectList(_context.MCategory, "ID", "Name", mainTransaction.MCategoryId);
            ViewData["MpocketId"] = new SelectList(_context.MPocket, "ID", "Name", mainTransaction.MpocketId);
            return View(mainTransaction);
        }

        // POST: MainTransactions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,TransactionDate,MpocketId,MCategoryId,Detail,Amount,StatusFlag,LastUpdate")] MainTransaction mainTransaction)
        {
            return NotFound();

            if (id != mainTransaction.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mainTransaction);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MainTransactionExists(mainTransaction.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MCategoryId"] = new SelectList(_context.MCategory, "ID", "Name", mainTransaction.MCategoryId);
            ViewData["MpocketId"] = new SelectList(_context.MPocket, "ID", "Name", mainTransaction.MpocketId);
            return View(mainTransaction);
        }

        // GET: MainTransactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            var mainTransaction = await _context.MainTransaction
                .Include(m => m.MCategory)
                .Include(m => m.MPocket)
                .FirstOrDefaultAsync(m => m.ID == id && m.User == user);
            if (mainTransaction == null)
            {
                return NotFound();
            }

            return View(mainTransaction);
        }

        // POST: MainTransactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mainTransaction = await _context.MainTransaction.FindAsync(id);
            _context.MainTransaction.Remove(mainTransaction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MainTransactionExists(int id)
        {
            return _context.MainTransaction.Any(e => e.ID == id);
        }
    }
}

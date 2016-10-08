﻿namespace RealEstate.Rentals
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Web.Mvc;
	using App_Start;
	using MongoDB.Bson;
	using MongoDB.Driver;
	using MongoDB.Driver.Builders;
	using MongoDB.Driver.Linq;

	public class RentalsController : Controller
	{
		public readonly RealEstateContext Context = new RealEstateContext();

		public ActionResult Index(RentalsFilter filters)
		{
			var rentals = FilterRentals(filters);
			var model = new RentalsList
			{
				Rentals = rentals,
				Filters = filters
			};
			return View(model);
		}

		private IEnumerable<Rental> FilterRentals(RentalsFilter filters)
		{
			IQueryable<Rental> rentals = Context.Rentals.AsQueryable()
				.OrderBy(r => r.Price);

			if (filters.MinimumRooms.HasValue)
			{
				rentals = rentals
					.Where(r => r.NumberOfRooms >= filters.MinimumRooms);
			}

			if (filters.PriceLimit.HasValue)
			{
				var query = Query<Rental>.LTE(r => r.Price, filters.PriceLimit);
				rentals = rentals
					.Where(r => query.Inject());
			}

			return rentals;
		}

		public ActionResult Post()
		{
			return View();
		}

		[HttpPost]
		public ActionResult Post(PostRental postRental)
		{
			var rental = new Rental(postRental);
			Context.Rentals.Insert(rental);
			return RedirectToAction("Index");
		}

		public ActionResult AdjustPrice(string id)
		{
			var rental = GetRental(id);
			return View(rental);
		}

		private Rental GetRental(string id)
		{
			var rental = Context.Rentals.FindOneById(new ObjectId(id));
			return rental;
		}

		[HttpPost]
		public ActionResult AdjustPrice(string id, AdjustPrice adjustPrice)
		{
			var rental = GetRental(id);
			rental.AdjustPrice(adjustPrice);
			Context.Rentals.Save(rental);
			return RedirectToAction("Index");
		}

		[HttpPost]
		public ActionResult AdjustPriceUsingModification(string id, AdjustPrice adjustPrice)
		{
			var rental = GetRental(id);
			var adjustment = new PriceAdjustment(adjustPrice, rental.Price);
			var modificationUpdate = Update<Rental>
				.Push(r => r.Adjustments, adjustment)
				.Set(r => r.Price, adjustPrice.NewPrice);
			Context.Rentals.Update(Query.EQ("_id", new ObjectId(id)), modificationUpdate);
			return RedirectToAction("Index");
		}

		public ActionResult Delete(string id)
		{
			Context.Rentals.Remove(Query.EQ("_id", new ObjectId(id)));
			return RedirectToAction("Index");
		}

		public string PriceDistribution()
		{
			return new QueryPriceDistribution()
				.Run(Context.Rentals)
				.ToJson();
		}
	}
}
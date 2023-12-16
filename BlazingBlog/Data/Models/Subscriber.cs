﻿using System.ComponentModel.DataAnnotations;

namespace BlazingBlog.Data.Models
{
	public class Subscriber
	{
		public int Id { get; set; }

		[EmailAddress, Required, MaxLength(150)]
		public string Email { get; set; }

		[Required, MaxLength(25)]
		public string Name { get; set; }
		public DateTime SubscribedOn { get; set; }
	}
}

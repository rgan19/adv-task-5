﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TaskProcessor.Model
{
	public class TaskItem
	{
		[Required]
		public int taskID { get; set; }
		public int customerID { get; set; }
		public String description { get; set; }
		public String priority { get; set; }
		[Required]
		[EnumDataType(typeof(StatusTypes))]
		[JsonConverter(typeof(StringEnumConverter))]
		public StatusTypes status { get; set; }
	}

	public enum StatusTypes
	{
		[Description("STARTED")]
		STARTED,

		[Description("IN_PROGRESS")]
		IN_PROGRESS,

		[Description("COMPLETED")]
		COMPLETED,

		[Description("FAILED")]
		FAILED

	}
}



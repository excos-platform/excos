using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class DiffContextBO
	{
		public DiffContextBO()
		{
			this.Parameters = new Dictionary<string, string>();
			this.IsResponse = false;
			this.IsRequest = true;
		}

		public string URL { get; set; }
		public Dictionary<string, string> Parameters { get; set; }
		public OperationType Method { get; private set; }
		public bool IsResponse { get; private set; }
		public bool IsRequest { get; private set; }

		public bool IsRequired { get; private set; }

		public string GetDiffContextElementType() => this.IsResponse ? "Response" : "Request";


		public DiffContextBO CopyWithMethod(OperationType method)
		{
			var result = this.Copy();
			result.Method = method;
			return result;
		}

		public DiffContextBO CopyWithRequired(bool required)
		{
			var result = this.Copy();
			result.IsRequired = required;
			return result;
		}

		public DiffContextBO CopyAsRequest()
		{
			var result = this.Copy();
			result.IsRequest = true;
			result.IsResponse = false;
			return result;
		}

		public DiffContextBO CopyAsResponse()
		{
			var result = this.Copy();
			result.IsResponse = true;
			result.IsRequest = false;
			return result;
		}

		private DiffContextBO Copy()
		{
			var context = new DiffContextBO
			{
				URL = this.URL,
				Parameters = this.Parameters,
				Method = this.Method,
				IsResponse = this.IsResponse,
				IsRequest = this.IsRequest,
				IsRequired = this.IsRequired
			};
			return context;
		}

		public override bool Equals(object o)
		{
			if (this == o) return true;

			if (o == null || this.GetType() != o.GetType()) return false;

			var that = (DiffContextBO)o;

			return this.IsResponse.Equals(that.IsResponse)
				   && this.IsRequest.Equals(that.IsRequest)
				   && this.URL.Equals(that.URL)
				   && this.Parameters.Equals(that.Parameters)
				   && this.Method.Equals(that.Method)
				   && this.IsRequired.Equals(that.IsRequired);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(this.URL, this.Parameters, this.Method, this.IsResponse, this.IsRequest, this.IsRequired);
		}
	}
}
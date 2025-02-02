// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using System.Diagnostics;
using System.Reflection;
using Excos.Platform.Common.Privacy.Redaction;
using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Frames;
using JasperFx.CodeGeneration.Model;
using JasperFx.Core.Reflection;
using Wolverine;
using Wolverine.Configuration;

namespace Excos.Platform.Common.Wolverine.Telemetry;

internal class EventLoggingFrame : SyncFrame
{
	private readonly Type inputType;
	private readonly List<PrivacyValueDescriptor> members;
	private Variable? input;
	private Variable? redactor;
	private Variable? envelope;

	public EventLoggingFrame(IChain chain)
	{
		this.inputType = chain.InputType()!;
		this.members = this.inputType.GetProperties().Cast<MemberInfo>().Concat(this.inputType.GetFields())
			.Select(PrivacyValueDescriptor.CreateOrNull)
			.Where(x => x != null).Cast<PrivacyValueDescriptor>()
			.ToList();
	}

	public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
	{
		this.input = chain.FindVariable(this.inputType);
		yield return this.input;

		this.redactor = chain.FindVariable(typeof(PrivacyValueRedactor));
		yield return this.redactor;

		this.envelope = chain.FindVariable(typeof(Envelope));
		yield return this.envelope;
	}

	public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
	{
		writer.WriteComment("Application-specific Open Telemetry event logging");

		writer.WriteLine($"if ({typeof(Activity).FullNameInCode()}.{nameof(Activity.Current)} != null) {{");
		writer.IndentionLevel++;
		writer.WriteLine($"var eventTags = new {typeof(ActivityTagsCollection).FullNameInCode()}();");
		// No point in logging those as they are already present in the activity
		//writer.WriteLine($"eventTags.{nameof(ActivityTagsCollection.Add)}(\"EnvelopeId\", {this.envelope!.Usage}.{nameof(Envelope.Id)});");
		//writer.WriteLine($"eventTags.{nameof(ActivityTagsCollection.Add)}(\"{nameof(Envelope.ConversationId)}\", {this.envelope!.Usage}.{nameof(Envelope.ConversationId)});");
		//writer.WriteLine($"eventTags.{nameof(ActivityTagsCollection.Add)}(\"{nameof(Envelope.CorrelationId)}\", {this.envelope!.Usage}.{nameof(Envelope.CorrelationId)});");
		//writer.WriteLine($"eventTags.{nameof(ActivityTagsCollection.Add)}(\"{nameof(Envelope.TenantId)}\", {this.envelope!.Usage}.{nameof(Envelope.TenantId)});");

		foreach (PrivacyValueDescriptor member in this.members)
			writer.WriteLine(
				$"eventTags.{nameof(ActivityTagsCollection.Add)}(\"{member.OpenTelemetryName}\", {this.redactor!.Usage}.{nameof(PrivacyValueRedactor.Redact)}({this.input!.Usage}.{member.Source.Name}, {typeof(PrivacyValueRedaction).FullNameInCode()}.{member.Redaction}));");
		writer.WriteLine($"{typeof(Activity).FullNameInCode()}.{nameof(Activity.Current)}.{nameof(Activity.AddEvent)}(new {typeof(ActivityEvent).FullNameInCode()}(\"{PrivacyValueDescriptor.GetDisplayName(this.inputType)}\", tags: eventTags));");
		writer.IndentionLevel--;
		writer.WriteLine("}");

		this.Next?.GenerateCode(method, writer);
	}
}
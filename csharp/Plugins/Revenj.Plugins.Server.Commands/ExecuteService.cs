﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Principal;
using Revenj.Common;
using Revenj.DomainPatterns;
using Revenj.Extensibility;
using Revenj.Processing;
using Revenj.Security;
using Revenj.Serialization;
using Revenj.Utility;

namespace Revenj.Plugins.Server.Commands
{
	[Export(typeof(IServerCommand))]
	[ExportMetadata(Metadata.ClassType, typeof(ExecuteService))]
	public class ExecuteService : IServerCommand
	{
		private static Dictionary<Type, IExecuteCommand> Cache = new Dictionary<Type, IExecuteCommand>();

		private readonly ITypeResolver TypeResolver;
		private readonly IPermissionManager Permissions;

		public ExecuteService(
			ITypeResolver typeResolver,
			IPermissionManager permissions)
		{
			Contract.Requires(typeResolver != null);
			Contract.Requires(permissions != null);

			this.TypeResolver = typeResolver;
			this.Permissions = permissions;
		}

		[DataContract(Namespace = "")]
		public class Argument<TFormat>
		{
			[DataMember]
			public string Name;
			[DataMember]
			public TFormat Data;
		}

		private static TFormat CreateExampleArgument<TFormat>(ISerialization<TFormat> serializer)
		{
			return serializer.Serialize(new Argument<TFormat> { Name = "CheckInfo" });
		}

		public ICommandResult<TOutput> Execute<TInput, TOutput>(
			IServiceProvider locator,
			ISerialization<TInput> input,
			ISerialization<TOutput> output,
			IPrincipal principal,
			TInput data)
		{
			var either = CommandResult<TOutput>.Check<Argument<TInput>, TInput>(input, output, data, CreateExampleArgument);
			if (either.Error != null)
				return either.Error;
			var argument = either.Argument;

			var serviceType = TypeResolver.Resolve(argument.Name);
			if (serviceType == null)
				return
					CommandResult<TOutput>.Fail(
						"Couldn't find service: {0}".With(argument.Name),
						@"Example argument: 
" + CommandResult<TOutput>.ConvertToString(CreateExampleArgument(output)));

			var serviceInterface =
				serviceType.GetInterfaces()
				.FirstOrDefault(it => it.IsGenericType && typeof(IServerService<,>) == it.GetGenericTypeDefinition());
			if (serviceInterface == null)
				return
					CommandResult<TOutput>.Fail(
						"Object: {0} is not a valid service.".With(argument.Name),
						"{0} must implement {1} to be executed as a service call".With(argument.Name, typeof(IServerService<,>).FullName));

			if (!Permissions.CanAccess(serviceType.FullName, principal))
				return CommandResult<TOutput>.Forbidden(argument.Name);
			try
			{
				IExecuteCommand command;
				if (!Cache.TryGetValue(serviceType, out command))
				{
					var commandType = typeof(ExecuteServiceCommand<,>).MakeGenericType(serviceInterface.GetGenericArguments());
					command = Activator.CreateInstance(commandType) as IExecuteCommand;
					var newCache = new Dictionary<Type, IExecuteCommand>(Cache);
					newCache[serviceType] = command;
					Cache = newCache;
				}
				var result = command.Execute(input, output, locator, serviceType, argument.Data);

				return CommandResult<TOutput>.Return(HttpStatusCode.Created, result, "Service executed");
			}
			catch (ArgumentException ex)
			{
				return CommandResult<TOutput>.Fail(
					ex.Message,
					ex.GetDetailedExplanation() + @"
Example argument: 
" + CommandResult<TOutput>.ConvertToString(CreateExampleArgument(output)));
			}
		}

		private interface IExecuteCommand
		{
			TOutput Execute<TInput, TOutput>(
				ISerialization<TInput> input,
				ISerialization<TOutput> output,
				IServiceProvider locator,
				Type serviceType,
				TInput data);
		}

		private class ExecuteServiceCommand<TArgument, TResult> : IExecuteCommand
		{
			public TOutput Execute<TInput, TOutput>(
				ISerialization<TInput> input,
				ISerialization<TOutput> output,
				IServiceProvider locator,
				Type serviceType,
				TInput data)
			{
				TArgument arg;
				try
				{
					arg = data != null ? input.Deserialize<TInput, TArgument>(data, locator) : Activator.CreateInstance<TArgument>();
				}
				catch (Exception ex)
				{
					throw new ArgumentException("Error deserializing service argument.", ex);
				}
				IServerService<TArgument, TResult> service;
				try
				{
					service = locator.Resolve<IServerService<TArgument, TResult>>(serviceType);
				}
				catch (Exception ex)
				{
					throw new ArgumentException("Can't create service instance.", ex);
				}
				try
				{
					return output.Serialize(service.Execute(arg));
				}
				catch (SecurityException) { throw; }
				catch (Exception ex)
				{
					string additionalInfo;
					try
					{
						additionalInfo = @"Sent data:
" + input.Serialize(arg);
					}
					catch (Exception sex)
					{
						additionalInfo = "Error serializing input: " + sex.Message;
					}
					throw new ArgumentException(
						ex.Message,
						new FrameworkException(@"Error while executing service: {0}. {1}".With(ex.Message, additionalInfo), ex));
				}
			}
		}
	}
}

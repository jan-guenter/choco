// Copyright © 2017 - 2021 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.app.registration
{
    using infrastructure.events;
    using infrastructure.tasks;
    using NuGet;
    using SimpleInjector;
    using adapters;
    using filesystem;
    using infrastructure.commands;
    using infrastructure.configuration;
    using infrastructure.services;
    using infrastructure.validations;
    using nuget;
    using services;
    using Assembly = System.Reflection.Assembly;
    using CryptoHashProvider = cryptography.CryptoHashProvider;
    using IFileSystem = filesystem.IFileSystem;
    using IHashProvider = cryptography.IHashProvider;

    // ReSharper disable InconsistentNaming

    /// <summary>
    ///   The main inversion container registration for the application. Look for other container bindings in client projects.
    /// </summary>
    public sealed class ContainerBinding
    {
        /// <summary>
        ///   Loads the module into the kernel.
        /// </summary>
        public void RegisterComponents(Container container)
        {
            var configuration = Config.get_configuration_settings();

            container.RegisterSingleton(() => configuration);
            container.RegisterSingleton<IFileSystem, DotNetFileSystem>();
            container.RegisterSingleton<IXmlService, XmlService>();
            container.RegisterSingleton<IDateTimeService, SystemDateTimeUtcService>();

            //nuget
            container.RegisterSingleton<ILogger, ChocolateyNugetLogger>();
            container.RegisterSingleton<INugetService, NugetService>();
            container.RegisterSingleton<IPackageDownloader, PackageDownloader>();
            container.RegisterSingleton<IPowershellService, PowershellService>();
            container.RegisterSingleton<IChocolateyPackageInformationService, ChocolateyPackageInformationService>();
            container.RegisterSingleton<IShimGenerationService, ShimGenerationService>();
            container.RegisterSingleton<IRegistryService, RegistryService>();
            container.RegisterSingleton<IPendingRebootService, PendingRebootService>();
            container.RegisterSingleton<IFilesService, FilesService>();
            container.RegisterSingleton<IConfigTransformService, ConfigTransformService>();
            container.RegisterSingleton<IHashProvider>(() => new CryptoHashProvider(container.GetInstance<IFileSystem>()));
            container.RegisterSingleton<ITemplateService, TemplateService>();
            container.RegisterSingleton<IChocolateyConfigSettingsService, ChocolateyConfigSettingsService>();
            container.RegisterSingleton<IChocolateyPackageService, ChocolateyPackageService>();
            container.RegisterSingleton<IAutomaticUninstallerService, AutomaticUninstallerService>();
            container.RegisterSingleton<ICommandExecutor, CommandExecutor>();
            container.RegisterSingleton(() => new CustomString(string.Empty));

            container.Collection.Register<ICommand>(new[]{Assembly.GetAssembly(typeof(ICommand))}, Lifestyle.Singleton);

            container.Collection.Register<ISourceRunner>(new[]{Assembly.GetAssembly(typeof(ISourceRunner))}, Lifestyle.Singleton);

            container.Register<IEventSubscriptionManagerService, EventSubscriptionManagerService>(Lifestyle.Singleton);
            EventManager.initialize_with(container.GetInstance<IEventSubscriptionManagerService>);

            container.Collection.Register<ITask>(new[]{Assembly.GetAssembly(typeof(ITask))}, Lifestyle.Singleton);

            container.Collection.Register<IValidation>(new[]{Assembly.GetAssembly(typeof(IValidation))}, Lifestyle.Singleton);
        }
    }

    // ReSharper restore InconsistentNaming
}
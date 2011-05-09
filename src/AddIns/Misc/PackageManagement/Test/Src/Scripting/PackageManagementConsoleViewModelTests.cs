﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.PackageManagement;
using ICSharpCode.PackageManagement.Design;
using ICSharpCode.PackageManagement.Scripting;
using ICSharpCode.SharpDevelop.Project;
using NuGet;
using NUnit.Framework;
using PackageManagement.Tests.Helpers;

namespace PackageManagement.Tests.Scripting
{
	[TestFixture]
	public class PackageManagementConsoleViewModelTests
	{
		TestablePackageManagementConsoleViewModel viewModel;
		FakePackageManagementProjectService projectService;
		FakePackageManagementConsoleHost consoleHost;
		List<string> propertiesChanged;
		
		void CreateConsoleHost()
		{
			consoleHost = new FakePackageManagementConsoleHost();
			projectService = consoleHost.FakeProjectService;
		}
		
		void CreateViewModel()
		{
			CreateConsoleHost();
			CreateViewModel(consoleHost);
		}
		
		void CreateViewModel(IPackageManagementConsoleHost consoleHost)
		{
			viewModel = new TestablePackageManagementConsoleViewModel(consoleHost);
		}
		
		void CreateViewModel(IEnumerable<PackageSource> packageSources, IPackageManagementConsoleHost consoleHost)
		{
			viewModel = new TestablePackageManagementConsoleViewModel(packageSources, consoleHost);
		}
		
		void CreateViewModelWithOneRegisteredPackageSource()
		{
			CreateConsoleHost();
			var packageSources = new List<PackageSource>();
			packageSources.Add(new PackageSource("First source"));
			CreateViewModel(packageSources, consoleHost);
		}
		
		void CreateViewModelWithTwoRegisteredPackageSources()
		{
			CreateConsoleHost();
			var packageSources = new List<PackageSource>();
			packageSources.Add(new PackageSource("First source"));
			packageSources.Add(new PackageSource("Second source"));
			CreateViewModel(packageSources, consoleHost);
		}
		
		PackageSource CreateViewModelWithTwoRegisteredPackageSourcesAndLastOneIsActivePackageSource()
		{
			CreateConsoleHost();
			var packageSources = new List<PackageSource>();
			packageSources.Add(new PackageSource("First source"));
			var activePackageSource = new PackageSource("Second source");
			packageSources.Add(activePackageSource);
			
			consoleHost.ActivePackageSource = activePackageSource;
			
			CreateViewModel(packageSources, consoleHost);
			
			return activePackageSource;
		}
		
		Solution CreateViewModelWithOneProjectOpen()
		{
			CreateConsoleHost();
			Solution solution = CreateSolutionWithOneProject();
			projectService.OpenSolution = solution;
			CreateViewModel(consoleHost);
			
			return solution;
		}
		
		Solution CreateSolutionWithOneProject()
		{
			TestableProject project = ProjectHelper.CreateTestProject();
			Solution solution = project.ParentSolution;
			solution.AddFolder(project);
			
			return solution;
		}
		
		PackageSource AddOnePackageSourceAndRemoveAnyExistingPackageSources()
		{
			viewModel.RegisteredPackageSources.Clear();
			return AddOnePackageSource();
		}
		
		PackageSource AddOnePackageSource()
		{
			return AddOnePackageSource("NewSource");
		}
		
		PackageSource AddOnePackageSource(string name)
		{
			var source = new PackageSource(name);
			viewModel.RegisteredPackageSources.Add(source);
			return source;
		}
		
		PackageSourceViewModel SelectSecondPackageSource()
		{
			var selectedPackageSource = viewModel.PackageSources[1];
			viewModel.ActivePackageSource = selectedPackageSource;
			return selectedPackageSource;
		}

		void RecordPropertyChangedEvents()
		{
			propertiesChanged = new List<string>();
			viewModel.PropertyChanged += (sender, e) => propertiesChanged.Add(e.PropertyName);
		}
		
		Solution CreateConsoleHostWithEmptySolutionOpen()
		{
			CreateConsoleHost();
			var solution = new Solution();
			projectService.OpenSolution = solution;
			return solution;
		}
		
		Solution CreateViewModelWithEmptySolutionOpen()
		{
			var solution = CreateConsoleHostWithEmptySolutionOpen();
			CreateViewModel(consoleHost);
			return solution;
		}
		
		TestableProject AddProjectToSolution(Solution solution)
		{
			var project = ProjectHelper.CreateTestProject();
			solution.AddFolder(project);
			return project;
		}
		
		void CloseSolution()
		{
			projectService.OpenSolution = null;
			projectService.FireSolutionClosedEvent();
		}
		
		void OpenSolution(Solution solution)
		{
			projectService.OpenSolution = solution;
			projectService.FireSolutionLoadedEvent(solution);
		}
		
		IProject RemoveProjectFromSolution(Solution solution)
		{
			var project = solution.Projects.FirstOrDefault();
			solution.RemoveFolder(project);
			return project;
		}

		[Test]
		public void PackageSources_OneRegisteredPackageSourceWhenConsoleCreated_OnePackageSourceDisplayed()
		{
			CreateViewModelWithOneRegisteredPackageSource();
			
			var expectedPackageSources = viewModel.RegisteredPackageSources;
			var actualPackageSources = viewModel.PackageSources;
			
			PackageSourceCollectionAssert.AreEqual(expectedPackageSources, actualPackageSources);
		}
		
		[Test]
		public void ActivePackageSource_OneRegisteredPackageSourceWhenConsoleCreated_SinglePackageSourceIsActivePackageSource()
		{
			CreateViewModelWithOneRegisteredPackageSource();
			var expectedPackageSource = viewModel.RegisteredPackageSources[0];
			var actualPackageSource = viewModel.ActivePackageSource.GetPackageSource();
			
			Assert.AreEqual(expectedPackageSource, actualPackageSource);
		}
		
		[Test]
		public void ActivePackageSource_TwoRegisteredPackageSourcesAndLastOneIsActiveWhenConsoleCreated_SecondPackageSourceIsActiveInViewModel()
		{
			var expectedPackageSource = CreateViewModelWithTwoRegisteredPackageSourcesAndLastOneIsActivePackageSource();
			
			var actualPackageSource = viewModel.ActivePackageSource.GetPackageSource();
			
			Assert.AreEqual(expectedPackageSource, actualPackageSource);
		}
		
		[Test]
		public void PackageSources_OriginalPackageSourceRemovedAndOnePackageSourceAddedAfterConsoleCreated_NewPackageSourceIsDisplayed()
		{
			CreateViewModel();
			AddOnePackageSourceAndRemoveAnyExistingPackageSources();
			
			var expectedPackageSources = viewModel.RegisteredPackageSources;
			var actualPackageSources = viewModel.PackageSources;
			
			PackageSourceCollectionAssert.AreEqual(expectedPackageSources, actualPackageSources);
		}
		
		[Test]
		public void PackageSources_TwoRegisteredPackageSourcesWhenConsoleCreated_ThreePackageSourcesReturnedIncludingAggregatePackageSource()
		{
			CreateViewModelWithTwoRegisteredPackageSources();
			
			var actualPackageSources = viewModel.PackageSources;
			
			var expectedPackageSources = new List<PackageSource>(viewModel.RegisteredPackageSources);
			expectedPackageSources.Add(RegisteredPackageSourceSettings.AggregatePackageSource);
			
			PackageSourceCollectionAssert.AreEqual(expectedPackageSources, actualPackageSources);
		}
		
		[Test]
		public void ActivePackageSource_OriginalPackageSourceRemovedAndOnePackageSourceAddedAfterConsoleCreated_ActivePackageSourceIsUpdatedToNewPackageSource()
		{
			CreateViewModel();
			AddOnePackageSourceAndRemoveAnyExistingPackageSources();
			
			var expectedPackageSource = viewModel.RegisteredPackageSources[0];
			var actualPackageSource = viewModel.ActivePackageSource.GetPackageSource();
			
			Assert.AreEqual(expectedPackageSource, actualPackageSource);
		}
		
		[Test]
		public void ActivePackageSource_OriginalPackageSourceRemovedAndOnePackageSourceAddedAfterConsoleCreated_PropertyChangedEventFiredForActivePackageSource()
		{
			CreateViewModel();
			RecordPropertyChangedEvents();
			AddOnePackageSourceAndRemoveAnyExistingPackageSources();
			
			bool result = propertiesChanged.Contains("ActivePackageSource");
			Assert.IsTrue(result);
		}
		
		[Test]
		public void ActivePackageSource_TwoPackageSourcesAndActivePackageSourceChangedToSecondOne_ActivePackageSourceChoiceIsRemembered()
		{
			CreateViewModel();
			AddOnePackageSource();
			var selectedPackageSource = SelectSecondPackageSource();
			
			var actualPackageSource = viewModel.ActivePackageSource;
			
			Assert.AreEqual(selectedPackageSource, actualPackageSource);
		}
		
		[Test]
		public void ActivePackageSource_SelectedPackageSourceIsRemoved_ActivePackageSourceIsChangedToFirstPackageSource()
		{
			CreateViewModel();
			AddOnePackageSource();
			SelectSecondPackageSource();
			viewModel.RegisteredPackageSources.RemoveAt(1);
			
			var expectedPackageSource = viewModel.PackageSources[0];
			var actualPackageSource = viewModel.ActivePackageSource;
			
			Assert.AreEqual(expectedPackageSource, actualPackageSource);
		}
		
		[Test]
		public void Projects_OneProjectOpenWhenConsoleCreated_OpenProjectIsInProjectsCollection()
		{
			var solution = CreateViewModelWithOneProjectOpen();
			var expectedProjects = solution.Projects;
			var actualProjects = viewModel.Projects;
			
			CollectionAssert.AreEqual(expectedProjects, actualProjects);
		}
		
		[Test]
		public void DefaultProject_OneProjectOpenWhenConsoleCreated_OpenProjectIsDefaultProject()
		{
			var solution = CreateViewModelWithOneProjectOpen();
			var expectedProject = solution.Projects.First();
			var actualProject = viewModel.DefaultProject;
			
			Assert.AreEqual(expectedProject, actualProject);
		}
		
		[Test]
		public void Constructor_EmptySolutionOpenWhenConsoleCreated_DoesNotThrowException()
		{
			CreateConsoleHostWithEmptySolutionOpen();
			Assert.DoesNotThrow(() => CreateViewModel(consoleHost));
		}
		
		[Test]
		public void Projects_ProjectAddedToEmptySolution_ProjectDisplayed()
		{
			var solution = CreateViewModelWithEmptySolutionOpen();
			var project = AddProjectToSolution(solution);
			projectService.FireProjectAddedEvent(project);
			
			var actualProjects = viewModel.Projects;
			var expectedProjects = solution.Projects;
			
			CollectionAssert.AreEqual(expectedProjects, actualProjects);
		}
		
		[Test]
		public void DefaultProject_ProjectAddedToEmptySolution_ProjectAddedIsDefaultProject()
		{
			var solution = CreateViewModelWithEmptySolutionOpen();
			var project = AddProjectToSolution(solution);
			projectService.FireProjectAddedEvent(project);
			
			var actualProject = viewModel.DefaultProject;
			
			Assert.AreEqual(project, actualProject);
		}
		
		[Test]
		public void Projects_SolutionClosed_ProjectsRemovedFromList()
		{
			CreateViewModelWithOneProjectOpen();
			CloseSolution();
			
			int count = viewModel.Projects.Count;
			
			Assert.AreEqual(0, count);
		}
		
		[Test]
		public void DefaultProject_SolutionClosed_DefaultProjectIsSetToNull()
		{
			CreateViewModelWithOneProjectOpen();
			CloseSolution();
			
			Assert.IsNull(viewModel.DefaultProject);
		}
		
		[Test]
		public void DefaultProject_SolutionClosed_PropertyChangedEventFiredForDefaultProject()
		{
			CreateViewModelWithOneProjectOpen();
			RecordPropertyChangedEvents();
			CloseSolution();
			
			bool result = propertiesChanged.Contains("DefaultProject");
			
			Assert.IsTrue(result);
		}
		
		[Test]
		public void Projects_SolutionWithOneProjectLoaded_ProjectsListUpdated()
		{
			CreateViewModel();
			var solution = CreateSolutionWithOneProject();
			OpenSolution(solution);
			
			var actualProjects = viewModel.Projects;
			var expectedProjects = solution.Projects;
			
			CollectionAssert.AreEqual(expectedProjects, actualProjects);
		}
		
		[Test]
		public void DefaultProject_SolutionWithOneProjectLoaded_DefaultProjectIsSetToProjectInSolution()
		{
			CreateViewModel();
			var solution = CreateSolutionWithOneProject();
			OpenSolution(solution);
			
			var actualProject = viewModel.DefaultProject;
			var expectedProject = viewModel.Projects[0];
			
			Assert.AreEqual(expectedProject, actualProject);
		}
		
		[Test]
		public void DefaultProject_SolutionWithOneProjectLoaded_PropertyChangedEventFiredForDefaultProjectI()
		{
			CreateViewModel();
			var solution = CreateSolutionWithOneProject();
			RecordPropertyChangedEvents();
			OpenSolution(solution);
			
			bool result = propertiesChanged.Contains("DefaultProject");
			
			Assert.IsTrue(result);
		}
		
		[Test]
		public void Projects_ProjectRemovedFromSolution_ProjectRemovedFromList()
		{
			var solution = CreateViewModelWithOneProjectOpen();
			var project = RemoveProjectFromSolution(solution);
			projectService.FireSolutionFolderRemoved(project);
			
			var actualProjects = viewModel.Projects;
			var expectedProjects = solution.Projects;
			
			CollectionAssert.AreEqual(expectedProjects, actualProjects);
		}
		
		[Test]
		public void DefaultProject_ProjectRemovedFromSolution_DefaultProjectIsUpdated()
		{
			var solution = CreateViewModelWithOneProjectOpen();
			var project = RemoveProjectFromSolution(solution);
			projectService.FireSolutionFolderRemoved(project);
			
			var actualProject = viewModel.DefaultProject;
			
			Assert.IsNull(actualProject);
		}
		
		[Test]
		public void Projects_SolutionFolderRemovedFromSolution_ProjectListIsUnchanged()
		{
			var solution = CreateViewModelWithOneProjectOpen();
			var solutionFolder = new SolutionFolder("Test", "Location", "Guid");
			projectService.FireSolutionFolderRemoved(solutionFolder);
			
			int count = viewModel.Projects.Count;
			
			Assert.AreEqual(1, count);
		}
		
		[Test]
		public void DefaultProject_OneProjectOpenWhenConsoleCreated_DefaultProjectSetForConsole()
		{
			var solution = CreateViewModelWithOneProjectOpen();
			var expectedProject = solution.Projects.First();
			var actualProject = consoleHost.DefaultProject;
			
			Assert.AreEqual(expectedProject, actualProject);
		}
		
		[Test]
		public void ActivePackageSource_OneRegisteredPackageSourceWhenConsoleCreated_ActivePackageSourceSetForConsole()
		{
			CreateViewModelWithOneRegisteredPackageSource();
			var expectedPackageSource = viewModel.RegisteredPackageSources[0];
			var actualPackageSource = consoleHost.ActivePackageSource;
			
			Assert.AreEqual(expectedPackageSource, actualPackageSource);
		}
		
		[Test]
		public void Dispose_ViewModelDisposed_ConsoleIsDisposed()
		{
			CreateViewModel();
			viewModel.Dispose();
			
			Assert.IsTrue(consoleHost.IsDisposeCalled);
		}
		
		[Test]
		public void ClearConsoleCommand_Executed_ClearsConsole()
		{
			CreateViewModel();
			viewModel.ClearConsoleCommand.Execute(null);
			
			Assert.IsTrue(consoleHost.IsClearCalled);
		}
		
		[Test]
		public void Constructor_NewViewModelCreated_ConsoleSetOnConsoleHost()
		{
			CreateViewModel();
			
			Assert.AreEqual(viewModel.FakeConsole, consoleHost.ScriptingConsole);
		}
		
		[Test]
		public void Constructor_NewViewModelCreated_ConsoleHostIsRun()
		{
			CreateViewModel();
			
			Assert.IsTrue(consoleHost.IsRunCalled);
		}
	}
}

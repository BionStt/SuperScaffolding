﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Internal;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Newtonsoft.Json;

namespace Microsoft.Extensions.ProjectModel
{
    public class MsBuildProjectContextBuilder
    {
        private string _projectPath;
        private string _targetLocation;
        private string _configuration;

        public MsBuildProjectContextBuilder(string projectPath, string targetsLocation, string configuration = "Debug")
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            if (string.IsNullOrEmpty(targetsLocation))
            {
                throw new ArgumentNullException(nameof(targetsLocation));
            }

            _configuration = configuration;
            _projectPath = projectPath;
            _targetLocation = targetsLocation;
        }

        public IProjectContext Build()
        {
            var errors = new List<string>();
            var output = new List<string>();
            var tmpFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var result = Command.CreateDotNet(
                "msbuild",
                new string[]
                {
                    _projectPath,
                    $"/t:EvaluateProjectInfoForCodeGeneration",
                    $"/p:OutputFile={tmpFile};CodeGenerationTargetLocation={_targetLocation};Configuration={_configuration}"
                })
                .OnErrorLine(e => errors.Add(e))
                .OnOutputLine(o => output.Add(o))
                .Execute();

            if (result.ExitCode != 0)
            {
                throw CreateProjectContextCreationFailedException(_projectPath, output, errors);
            }
            try
            {
                var info = File.ReadAllText(tmpFile);

                var buildContext = JsonConvert.DeserializeObject<CommonProjectContext>(info);

                return buildContext;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to read the BuildContext information.", ex);
            }
        }

        private Exception CreateProjectContextCreationFailedException(string projectPath, List<string> output, List<string> errors)
        {
            var message = $"Failed to get Project Context for {projectPath}.";

            if (output != null)
            {
                message += $"{Environment.NewLine} { string.Join(Environment.NewLine, output)} ";
            }

            if (errors != null)
            {
                message += $"{Environment.NewLine} { string.Join(Environment.NewLine, errors)} ";
            }

            return new InvalidOperationException(message);
        }
    }
}
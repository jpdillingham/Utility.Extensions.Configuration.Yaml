using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Utility.Extensions.Configuration.Yaml
{
    /// <summary>
    ///     Extension methods for adding <see cref="YamlConfigurationProvider"/>.
    /// </summary>
    public static class YamlConfigurationExtensions
    {
        /// <summary>
        ///     Adds the YAML configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Path relative to the base path stored in
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder, string path)
        {
            return AddYamlFile(builder, provider: null, path: path, optional: false, reloadOnChange: false);
        }

        /// <summary>
        ///     Adds the YAML configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Path relative to the base path stored in
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder, string path, bool optional)
        {
            return AddYamlFile(builder, provider: null, path: path, optional: optional, reloadOnChange: false);
        }

        /// <summary>
        ///     Adds the YAML configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Path relative to the base path stored in
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange)
        {
            return AddYamlFile(builder, provider: null, path: path, optional: optional, reloadOnChange: reloadOnChange);
        }

        /// <summary>
        ///     Adds a YAML configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="provider">The <see cref="IFileProvider"/> to use to access the file.</param>
        /// <param name="path">Path relative to the base path stored in
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder, IFileProvider provider, string path, bool optional, bool reloadOnChange)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("File path must be a non-empty string.", nameof(path));
            }

            return builder.AddYamlFile(s =>
            {
                s.FileProvider = provider;
                s.Path = path;
                s.Optional = optional;
                s.ReloadOnChange = reloadOnChange;
                s.ResolveFileProvider();
            });
        }

        /// <summary>
        ///     Adds a YAML configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="configureSource">Configures the source.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder, Action<YamlConfigurationSource> configureSource)
            => builder.Add(configureSource);
    }

    /// <summary>
    ///     Represents a YAML file as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class YamlConfigurationSource : FileConfigurationSource
    {
        /// <summary>
        /// Builds the <see cref="YamlConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="YamlConfigurationProvider"/></returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new YamlConfigurationProvider(this);
        }
    }

    /// <summary>
    ///     A YAML file based <see cref="FileConfigurationProvider"/>.
    /// </summary>
    public class YamlConfigurationProvider : FileConfigurationProvider
    {
        /// <summary>
        ///     Initializes a new instance with the specified source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public YamlConfigurationProvider(YamlConfigurationSource source) : base(source) { }

        private Dictionary<string, string> InternalData { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Loads the YAML data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        public override void Load(Stream stream)
        {
            try
            {
                var yaml = new YamlStream();
                using var reader = new StreamReader(stream);

                yaml.Load(reader);

                var rootNode = (YamlMappingNode)yaml.Documents[0].RootNode;

                Traverse(rootNode);

                Data = InternalData;
            }
            catch (YamlException e)
            {
                throw new FormatException("Could not parse the YAML file.", e);
            }
        }

        private void Traverse(YamlMappingNode root, string path = "")
        {
            string GetKey(KeyValuePair<YamlNode, YamlNode> node) 
                => ConfigurationPath.Combine(path, ((YamlScalarNode)node.Key).Value);

            foreach (var node in root.Children)
            {
                if (node.Value is YamlScalarNode)
                {
                    Console.WriteLine($"{GetKey(node)} = {node.Value.ToString()}");
                    InternalData[GetKey(node)] = node.Value.ToString();
                }

                if (node.Value is YamlMappingNode mappingNode)
                {
                    path += GetKey(node);
                    Traverse((YamlMappingNode)node.Value, path);
                }
            }
        }
    }
}

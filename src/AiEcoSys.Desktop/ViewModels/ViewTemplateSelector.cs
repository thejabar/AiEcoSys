using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace AiEcoSys.Desktop.ViewModels;

/// <summary>
/// Data template selector that routes views based on their key.
/// </summary>
public class ViewTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the dictionary mapping view keys to data templates.
    /// </summary>
    public Dictionary<string, DataTemplate> ViewTemplates { get; set; } = new();

    /// <inheritdoc />
    public override DataTemplate SelectTemplate(object? item, DependencyObject container)
    {
        if (item is not IViewMetadata viewMetadata || string.IsNullOrWhiteSpace(viewMetadata.ViewKey))
            return base.SelectTemplate(item, container);

        if (ViewTemplates.TryGetValue(viewMetadata.ViewKey, out var template))
            return template;

        return base.SelectTemplate(item, container);
    }
}

/// <summary>
/// Marker interface for views that want to participate in template selection.
/// </summary>
public interface IViewMetadata
{
    /// <summary>
    /// Gets the unique key for this view.
    /// </summary>
    string ViewKey { get; }
}
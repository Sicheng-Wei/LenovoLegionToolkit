﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using LenovoLegionToolkit.Lib.Automation.Pipeline;
using LenovoLegionToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.WPF.Controls.Automation.Steps;
using WPFUI.Common;
using WPFUI.Controls;
using Button = WPFUI.Controls.Button;

namespace LenovoLegionToolkit.WPF.Controls.Automation.Pipeline
{
    public class AutomationPipelineControl : UserControl
    {
        private readonly CardExpander _cardExpander = new()
        {
            Icon = SymbolRegular.Flow20,
            Margin = new(0, 0, 0, 8),
        };

        private readonly StackPanel _stackPanel = new();

        private readonly StackPanel _stepsStackPanel = new();

        private readonly StackPanel _buttonsStackPanel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new(0, 16, 0, 0),
        };

        private readonly Button _addStepButton = new()
        {
            Content = "Add step",
            Appearance = Appearance.Secondary,
            Width = 100,
            Margin = new(0, 0, 8, 0),
        };

        private readonly Button _deletePipelineButton = new()
        {
            Content = "Delete flow",
            Appearance = Appearance.Secondary,
            Width = 100,
        };
        public AutomationPipeline AutomationPipeline { get; }

        public event EventHandler? OnDelete;

        public AutomationPipelineControl(AutomationPipeline automationPipeline)
        {
            AutomationPipeline = automationPipeline;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _addStepButton.ContextMenu = GenerateAddStepContextMenu();
            _addStepButton.Click += (s, e) => _addStepButton.ContextMenu.IsOpen = true;

            _deletePipelineButton.Click += (s, e) => OnDelete?.Invoke(this, EventArgs.Empty);

            _buttonsStackPanel.Children.Add(_addStepButton);
            _buttonsStackPanel.Children.Add(_deletePipelineButton);

            _stackPanel.Children.Add(_stepsStackPanel);
            _stackPanel.Children.Add(_buttonsStackPanel);

            _cardExpander.Header = GenerateHeader();
            _cardExpander.Subtitle = GenerateSubtitle();
            _cardExpander.Content = _stackPanel;

            foreach (var step in AutomationPipeline.Steps)
            {
                var control = GenerateControl(step);
                _stepsStackPanel.Children.Add(control);
            }

            Content = _cardExpander;
        }

        private string GenerateHeader()
        {
            var parts = AutomationPipeline.Triggers.Select(t => t switch
            {
                AutomationPipelineTrigger.ACAdapterConnected => "AC adapter is connected",
                AutomationPipelineTrigger.ACAdapterDisconnected => "AC adapter is disconnected",
                _ => null,
            }).Where(s => !string.IsNullOrWhiteSpace(s));

            return "When " + string.Join(", ", parts);
        }

        private string GenerateSubtitle()
        {
            if (AutomationPipeline.Steps.Count == 1)
                return "1 action";
            return $"{AutomationPipeline.Steps.Count} actions";
        }

        private UIElement GenerateControl(IAutomationStep step)
        {
            AbstractAutomationStepControl control = step switch
            {
                DeactivateGPUAutomationStep s => new DeactivateGPUAutomationStepControl(s),
                OverDriveAutomationStep s => new OverDriveAutomationStepControl(s),
                PowerModeAutomationStep s => new PowerModeAutomationStepControl(s),
                RefreshRateAutomationStep s => new RefreshRateAutomationStepControl(s),
                _ => throw new InvalidOperationException("Unknown step type."),
            };
            control.OnDelete += (s, e) =>
            {
                if (s is AbstractAutomationStepControl step)
                    DeleteStep(step);
            };
            return control;
        }

        private ContextMenu GenerateAddStepContextMenu()
        {
            var powerMode = new MenuItem { Icon = SymbolRegular.Gauge24, Header = "Power Mode" };
            powerMode.Click += (s, e) => AddStep(new PowerModeAutomationStep(default));

            var refreshRate = new MenuItem { Icon = SymbolRegular.Laptop24, Header = "Refresh Rate" };
            refreshRate.Click += (s, e) => AddStep(new RefreshRateAutomationStep(default));

            var overDrive = new MenuItem { Icon = SymbolRegular.TopSpeed24, Header = "Over Drive" };
            overDrive.Click += (s, e) => AddStep(new OverDriveAutomationStep(default));

            var deactivateGPU = new MenuItem { Icon = SymbolRegular.DeveloperBoard24, Header = "Deactivate GPU" };
            deactivateGPU.Click += (s, e) => AddStep(new DeactivateGPUAutomationStep());

            return new()
            {
                PlacementTarget = _addStepButton,
                Placement = PlacementMode.Bottom,
                Items =
                {
                    powerMode,
                    refreshRate,
                    overDrive,
                    deactivateGPU,
                },
            };
        }

        private void AddStep(IAutomationStep step)
        {
            var control = GenerateControl(step);
            AutomationPipeline.Steps.Add(step);
            _stepsStackPanel.Children.Add(control);

            _cardExpander.Subtitle = GenerateSubtitle();
        }

        private void DeleteStep(AbstractAutomationStepControl control)
        {
            AutomationPipeline.Steps.Remove(control.AutomationStep);
            _stepsStackPanel.Children.Remove(control);

            _cardExpander.Subtitle = GenerateSubtitle();
        }
    }
}

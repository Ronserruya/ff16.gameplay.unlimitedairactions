﻿using ff16.gameplay.unlimitedairactions.Configuration;
using ff16.gameplay.unlimitedairactions.Template;

using Reloaded.Mod.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

using System.Diagnostics;
using Reloaded.Memory.Interfaces;


namespace ff16.gameplay.unlimitedairactions;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        PatchAirActionsCounter();
    }

    private void PatchAirActionsCounter() {
        _logger.WriteLine($"[{_modConfig.ModId}] Trying to override air action limit", _logger.ColorBlue);

        var startupScannerController = _modLoader.GetController<IStartupScanner>();
        if (startupScannerController == null || !startupScannerController.TryGetTarget(out var _startupScanner))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] startupScannerController not found, failed modifing air action limit!", _logger.ColorRed);
            return;
        }

        // sub_7ff6ef0a1a88
        _startupScanner.AddMainModuleScan("48 89 5C 24 08 57 48 83 EC 20 48 8B 01 48 8B D9 FF 41", res =>
        {
            if (!res.Found)
            {
                _logger.WriteLine($"[{_modConfig.ModId}] Air actions function not found, failed modifing air action limit!", _logger.ColorRed);
            }
            else
            {
                var funcAddr = Process.GetCurrentProcess().MainModule!.BaseAddress + res.Offset;
                var incAddr = funcAddr + 16;

                // Replace the instruction that increments the air action counter with noops
                // before: inc dword [rcx+0xc]
                // after: nop nop nop
                Span<byte> nopBytes = new byte[] { 0x90, 0x90, 0x90 };
                Reloaded.Memory.Memory.Instance.SafeWrite((nuint)incAddr, nopBytes);
                _logger.WriteLine($"[{_modConfig.ModId}] Sucesfully modified air action limiter at 0x{funcAddr:X}", _logger.ColorGreen);
            }
        });
    }

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}
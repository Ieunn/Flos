using Flos.Core.Messaging;

namespace Flos.Core.Sessions;

/// <summary>
/// Published after all modules have been loaded and initialized.
/// </summary>
public readonly record struct SessionInitializedMessage : IMessage;

/// <summary>
/// Published when the session transitions to <see cref="SessionState.Running"/>.
/// </summary>
public readonly record struct SessionStartedMessage : IMessage;

/// <summary>
/// Published when the session transitions to <see cref="SessionState.Paused"/>.
/// </summary>
public readonly record struct SessionPausedMessage : IMessage;

/// <summary>
/// Published when the session resumes from <see cref="SessionState.Paused"/>.
/// </summary>
public readonly record struct SessionResumedMessage : IMessage;

/// <summary>
/// Published during session shutdown, before modules are shut down.
/// Subscribers can react to the impending shutdown while services are still available.
/// </summary>
public readonly record struct SessionShutdownMessage : IMessage;

module FsMathFunctions.Portal.Models

open System

// ---------------------------------------------------------------------------
// Auth DTOs
// ---------------------------------------------------------------------------

type RegisterRequest =
    { email    : string
      password : string }

type LoginRequest =
    { email    : string
      password : string }

type LoginResponse =
    { token     : string
      expiresAt : DateTimeOffset }

// ---------------------------------------------------------------------------
// API Key DTOs
// ---------------------------------------------------------------------------

type CreateKeyRequest =
    { label : string }

/// Returned when a key is first generated — rawKey is shown ONCE only.
type CreateKeyResponse =
    { id        : Guid
      rawKey    : string
      prefix    : string
      label     : string
      createdAt : DateTimeOffset }

/// Safe representation of a key shown in listings (no rawKey).
type ApiKeyDto =
    { id        : Guid
      prefix    : string
      label     : string
      createdAt : DateTimeOffset
      revokedAt : DateTimeOffset option }

// ---------------------------------------------------------------------------
// Admin DTOs
// ---------------------------------------------------------------------------

type UserSummaryDto =
    { id       : Guid
      email    : string
      role     : string
      keyCount : int
      createdAt: DateTimeOffset }

// ---------------------------------------------------------------------------
// Error envelope (matches Finance API shape)
// ---------------------------------------------------------------------------

type ErrorDetail =
    { field   : string
      message : string }

type ErrorBody =
    { code    : string
      message : string
      details : ErrorDetail list }

type ErrorResponse =
    { error : ErrorBody }

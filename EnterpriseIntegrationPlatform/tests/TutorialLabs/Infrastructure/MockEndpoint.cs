// ============================================================================
// MockEndpoint – Re-exported from EnterpriseIntegrationPlatform.Testing library
// ============================================================================
// This file provides backward-compatible type aliases so that existing tutorials
// can continue to reference TutorialLabs.Infrastructure.MockEndpoint.
// The real implementation now lives in src/Testing/MockEndpoint.cs.
// ============================================================================

global using MockEndpoint = EnterpriseIntegrationPlatform.Testing.MockEndpoint;

namespace TutorialLabs.Infrastructure;

// Intentionally empty — the global using above re-exports MockEndpoint
// from the Testing library into the TutorialLabs.Infrastructure namespace.


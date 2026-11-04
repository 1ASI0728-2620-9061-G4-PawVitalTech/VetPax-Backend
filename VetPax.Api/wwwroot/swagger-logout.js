
(() => {
    "use strict";

    const realm = "vetpax";
    const clientId = "vetpax-dev";

    // Elegir Keycloak según dónde se abrió Swagger.
    const isLocal = [
        "localhost",
        "127.0.0.1"
    ].includes(window.location.hostname);

    const keycloakBaseUrl = isLocal
        ? "http://localhost:8080"
        : "https://keycloak-production-91a6.up.railway.app";

    const logoutUrl =
        `${keycloakBaseUrl}/realms/${encodeURIComponent(realm)}` +
        `/protocol/openid-connect/logout` +
        `?client_id=${encodeURIComponent(clientId)}`;

    // Escuchar el botón Logout del cuadro Authorize.
    document.addEventListener("click", (event) => {
        if (!(event.target instanceof Element)) {
            return;
        }

        const logoutButton = event.target.closest(
            '.swagger-ui .auth-container ' +
            'button[aria-label="Remove authorization"]'
        );

        if (!logoutButton) {
            return;
        }

        // Abrir el logout real de Keycloak en otra pestaña.
        window.open(
            logoutUrl,
            "_blank",
            "noopener,noreferrer"
        );

        // No detenemos el evento:
        // Swagger también elimina su autorización local.
    }, true);
})();

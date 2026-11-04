
(() => {
    "use strict";

    const isLocal = ["localhost", "127.0.0.1"]
        .includes(window.location.hostname);

    const keycloakBaseUrl = isLocal
        ? "http://localhost:8080"
        : "https://keycloak-production-91a6.up.railway.app";

    const logoutUrl =
        `${keycloakBaseUrl}/realms/vetpax` +
        "/protocol/openid-connect/logout" +
        "?client_id=vetpax-dev";

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

        // 1. Abrir el logout real de Keycloak
        // inmediatamente, para evitar bloqueos del navegador.
        window.open(
            logoutUrl,
            "_blank",
            "noopener,noreferrer"
        );

        // 2. Permitir que Swagger ejecute su Logout normal.
        // No detenemos el evento.

        // 3. Reiniciar Swagger UI para eliminar también
        // el estado OAuth2 y el código de autorización anterior.
        window.setTimeout(() => {
            window.location.reload();
        }, 250);
    }, true);
})();

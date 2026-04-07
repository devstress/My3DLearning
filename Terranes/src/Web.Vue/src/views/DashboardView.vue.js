/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/template-helpers.d.ts" />
/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/props-fallback.d.ts" />
import { ref, onMounted } from 'vue';
import { api } from '../api/client';
import LoadingSpinner from '../components/LoadingSpinner.vue';
import StatusBadge from '../components/StatusBadge.vue';
const DEMO_BUYER_ID = '00000000-0000-0000-0000-000000000001';
const activeJourneys = ref(null);
const notifications = ref(null);
const recentModels = ref(null);
const activeJourneyCount = ref(0);
const homeModelCount = ref(0);
const listingCount = ref(0);
const analyticsEventCount = ref(0);
onMounted(async () => {
    const [journeys, notifs, models, listings, analytics] = await Promise.all([
        api.getBuyerJourneys(DEMO_BUYER_ID),
        api.getNotifications(DEMO_BUYER_ID),
        api.getHomeModels(),
        api.getListings(),
        api.getAnalyticsCount(),
    ]);
    activeJourneys.value = journeys;
    activeJourneyCount.value = journeys.length;
    notifications.value = notifs;
    recentModels.value = models;
    homeModelCount.value = models.length;
    listingCount.value = listings.length;
    analyticsEventCount.value = analytics;
});
const __VLS_ctx = {
    ...{},
    ...{},
};
let __VLS_components;
let __VLS_intrinsics;
let __VLS_directives;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "container" },
});
/** @type {__VLS_StyleScopedClasses['container']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.h2, __VLS_intrinsics.h2)({
    ...{ class: "mb-4" },
});
/** @type {__VLS_StyleScopedClasses['mb-4']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "row g-4 mb-4" },
});
/** @type {__VLS_StyleScopedClasses['row']} */ ;
/** @type {__VLS_StyleScopedClasses['g-4']} */ ;
/** @type {__VLS_StyleScopedClasses['mb-4']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-3" },
});
/** @type {__VLS_StyleScopedClasses['col-md-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card shadow-sm text-center" },
});
/** @type {__VLS_StyleScopedClasses['card']} */ ;
/** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
/** @type {__VLS_StyleScopedClasses['text-center']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card-body" },
});
/** @type {__VLS_StyleScopedClasses['card-body']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.h3, __VLS_intrinsics.h3)({
    ...{ class: "text-primary" },
});
/** @type {__VLS_StyleScopedClasses['text-primary']} */ ;
(__VLS_ctx.activeJourneyCount);
__VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({
    ...{ class: "text-muted" },
});
/** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-3" },
});
/** @type {__VLS_StyleScopedClasses['col-md-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card shadow-sm text-center" },
});
/** @type {__VLS_StyleScopedClasses['card']} */ ;
/** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
/** @type {__VLS_StyleScopedClasses['text-center']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card-body" },
});
/** @type {__VLS_StyleScopedClasses['card-body']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.h3, __VLS_intrinsics.h3)({
    ...{ class: "text-success" },
});
/** @type {__VLS_StyleScopedClasses['text-success']} */ ;
(__VLS_ctx.homeModelCount);
__VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({
    ...{ class: "text-muted" },
});
/** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-3" },
});
/** @type {__VLS_StyleScopedClasses['col-md-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card shadow-sm text-center" },
});
/** @type {__VLS_StyleScopedClasses['card']} */ ;
/** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
/** @type {__VLS_StyleScopedClasses['text-center']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card-body" },
});
/** @type {__VLS_StyleScopedClasses['card-body']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.h3, __VLS_intrinsics.h3)({
    ...{ class: "text-info" },
});
/** @type {__VLS_StyleScopedClasses['text-info']} */ ;
(__VLS_ctx.listingCount);
__VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({
    ...{ class: "text-muted" },
});
/** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-3" },
});
/** @type {__VLS_StyleScopedClasses['col-md-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card shadow-sm text-center" },
});
/** @type {__VLS_StyleScopedClasses['card']} */ ;
/** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
/** @type {__VLS_StyleScopedClasses['text-center']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card-body" },
});
/** @type {__VLS_StyleScopedClasses['card-body']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.h3, __VLS_intrinsics.h3)({
    ...{ class: "text-warning" },
});
/** @type {__VLS_StyleScopedClasses['text-warning']} */ ;
(__VLS_ctx.analyticsEventCount);
__VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({
    ...{ class: "text-muted" },
});
/** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "row g-4" },
});
/** @type {__VLS_StyleScopedClasses['row']} */ ;
/** @type {__VLS_StyleScopedClasses['g-4']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-6" },
});
/** @type {__VLS_StyleScopedClasses['col-md-6']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card shadow-sm h-100" },
});
/** @type {__VLS_StyleScopedClasses['card']} */ ;
/** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
/** @type {__VLS_StyleScopedClasses['h-100']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card-header" },
});
/** @type {__VLS_StyleScopedClasses['card-header']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.strong, __VLS_intrinsics.strong)({});
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card-body" },
});
/** @type {__VLS_StyleScopedClasses['card-body']} */ ;
if (__VLS_ctx.activeJourneys === null) {
    const __VLS_0 = LoadingSpinner;
    // @ts-ignore
    const __VLS_1 = __VLS_asFunctionalComponent1(__VLS_0, new __VLS_0({}));
    const __VLS_2 = __VLS_1({}, ...__VLS_functionalComponentArgsRest(__VLS_1));
}
else if (__VLS_ctx.activeJourneys.length === 0) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
        ...{ class: "text-muted" },
    });
    /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
}
else {
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "list-group list-group-flush" },
    });
    /** @type {__VLS_StyleScopedClasses['list-group']} */ ;
    /** @type {__VLS_StyleScopedClasses['list-group-flush']} */ ;
    for (const [j] of __VLS_vFor((__VLS_ctx.activeJourneys.slice(0, 10)))) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            key: (j.id),
            ...{ class: "list-group-item d-flex justify-content-between align-items-center px-0" },
        });
        /** @type {__VLS_StyleScopedClasses['list-group-item']} */ ;
        /** @type {__VLS_StyleScopedClasses['d-flex']} */ ;
        /** @type {__VLS_StyleScopedClasses['justify-content-between']} */ ;
        /** @type {__VLS_StyleScopedClasses['align-items-center']} */ ;
        /** @type {__VLS_StyleScopedClasses['px-0']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.strong, __VLS_intrinsics.strong)({});
        (j.id.substring(0, 8));
        __VLS_asFunctionalElement1(__VLS_intrinsics.br)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({
            ...{ class: "text-muted" },
        });
        /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
        (new Date(j.startedUtc).toLocaleString());
        const __VLS_5 = StatusBadge;
        // @ts-ignore
        const __VLS_6 = __VLS_asFunctionalComponent1(__VLS_5, new __VLS_5({
            status: (j.currentStage),
        }));
        const __VLS_7 = __VLS_6({
            status: (j.currentStage),
        }, ...__VLS_functionalComponentArgsRest(__VLS_6));
        // @ts-ignore
        [activeJourneyCount, homeModelCount, listingCount, analyticsEventCount, activeJourneys, activeJourneys, activeJourneys,];
    }
}
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-6" },
});
/** @type {__VLS_StyleScopedClasses['col-md-6']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card shadow-sm h-100" },
});
/** @type {__VLS_StyleScopedClasses['card']} */ ;
/** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
/** @type {__VLS_StyleScopedClasses['h-100']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card-header" },
});
/** @type {__VLS_StyleScopedClasses['card-header']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.strong, __VLS_intrinsics.strong)({});
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card-body" },
});
/** @type {__VLS_StyleScopedClasses['card-body']} */ ;
if (__VLS_ctx.notifications === null) {
    const __VLS_10 = LoadingSpinner;
    // @ts-ignore
    const __VLS_11 = __VLS_asFunctionalComponent1(__VLS_10, new __VLS_10({}));
    const __VLS_12 = __VLS_11({}, ...__VLS_functionalComponentArgsRest(__VLS_11));
}
else if (__VLS_ctx.notifications.length === 0) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
        ...{ class: "text-muted" },
    });
    /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
}
else {
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "list-group list-group-flush" },
    });
    /** @type {__VLS_StyleScopedClasses['list-group']} */ ;
    /** @type {__VLS_StyleScopedClasses['list-group-flush']} */ ;
    for (const [n] of __VLS_vFor((__VLS_ctx.notifications.slice(0, 10)))) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            key: (n.id),
            ...{ class: "list-group-item px-0" },
            ...{ class: ({ 'fw-bold': !n.isRead }) },
        });
        /** @type {__VLS_StyleScopedClasses['list-group-item']} */ ;
        /** @type {__VLS_StyleScopedClasses['px-0']} */ ;
        /** @type {__VLS_StyleScopedClasses['fw-bold']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "d-flex justify-content-between" },
        });
        /** @type {__VLS_StyleScopedClasses['d-flex']} */ ;
        /** @type {__VLS_StyleScopedClasses['justify-content-between']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({});
        (n.title);
        __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
            ...{ class: "badge" },
            ...{ class: (!n.isRead ? 'bg-danger' : 'bg-secondary') },
        });
        /** @type {__VLS_StyleScopedClasses['badge']} */ ;
        (n.isRead ? 'Read' : 'Unread');
        __VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({
            ...{ class: "text-muted" },
        });
        /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
        (n.message);
        // @ts-ignore
        [notifications, notifications, notifications,];
    }
}
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "row g-4 mt-2" },
});
/** @type {__VLS_StyleScopedClasses['row']} */ ;
/** @type {__VLS_StyleScopedClasses['g-4']} */ ;
/** @type {__VLS_StyleScopedClasses['mt-2']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-12" },
});
/** @type {__VLS_StyleScopedClasses['col-12']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card shadow-sm" },
});
/** @type {__VLS_StyleScopedClasses['card']} */ ;
/** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card-header" },
});
/** @type {__VLS_StyleScopedClasses['card-header']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.strong, __VLS_intrinsics.strong)({});
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "card-body" },
});
/** @type {__VLS_StyleScopedClasses['card-body']} */ ;
if (__VLS_ctx.recentModels && __VLS_ctx.recentModels.length > 0) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "row g-3" },
    });
    /** @type {__VLS_StyleScopedClasses['row']} */ ;
    /** @type {__VLS_StyleScopedClasses['g-3']} */ ;
    for (const [model] of __VLS_vFor((__VLS_ctx.recentModels.slice(0, 4)))) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "col-md-3" },
            key: (model.id),
        });
        /** @type {__VLS_StyleScopedClasses['col-md-3']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card h-100" },
        });
        /** @type {__VLS_StyleScopedClasses['card']} */ ;
        /** @type {__VLS_StyleScopedClasses['h-100']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card-body" },
        });
        /** @type {__VLS_StyleScopedClasses['card-body']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.h6, __VLS_intrinsics.h6)({});
        (model.name);
        __VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({});
        (model.bedrooms);
        (model.bathrooms);
        __VLS_asFunctionalElement1(__VLS_intrinsics.br)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({
            ...{ class: "text-muted" },
        });
        /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
        (model.floorAreaSqm.toFixed(0));
        // @ts-ignore
        [recentModels, recentModels, recentModels,];
    }
}
else {
    __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
        ...{ class: "text-muted" },
    });
    /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
}
// @ts-ignore
[];
const __VLS_export = (await import('vue')).defineComponent({});
export default {};

/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/template-helpers.d.ts" />
/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/props-fallback.d.ts" />
import { ref, onMounted } from 'vue';
import { api } from '../api/client';
import StatusBadge from '../components/StatusBadge.vue';
import ErrorAlert from '../components/ErrorAlert.vue';
const DEMO_BUYER_ID = '00000000-0000-0000-0000-000000000001';
const currentJourney = ref(null);
const pastJourneys = ref(null);
const availableModels = ref(null);
const availableLand = ref(null);
const errorMessage = ref(null);
const journeyStages = [
    'Browsing', 'DesignSelected', 'PlacedOnLand',
    'Customising', 'QuoteRequested', 'QuoteReceived', 'Completed',
];
function getProgressPercent() {
    if (!currentJourney.value)
        return 0;
    const idx = journeyStages.indexOf(currentJourney.value.currentStage);
    return idx < 0 ? 0 : Math.round(((idx + 1) / journeyStages.length) * 100);
}
async function loadStageData() {
    errorMessage.value = null;
    if (currentJourney.value?.currentStage === 'Browsing') {
        availableModels.value = await api.getHomeModels();
    }
    else if (currentJourney.value?.currentStage === 'DesignSelected') {
        availableLand.value = await api.getLandBlocks();
    }
}
async function startJourney() {
    currentJourney.value = await api.createJourney(DEMO_BUYER_ID);
    await loadStageData();
}
async function selectDesign(modelId) {
    try {
        currentJourney.value = await api.advanceJourney(currentJourney.value.id, 'DesignSelected', modelId);
        await loadStageData();
    }
    catch (err) {
        errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
    }
}
async function selectLand(blockId) {
    try {
        currentJourney.value = await api.advanceJourney(currentJourney.value.id, 'PlacedOnLand', blockId);
        await loadStageData();
    }
    catch (err) {
        errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
    }
}
async function moveToCustomising() {
    try {
        currentJourney.value = await api.advanceJourney(currentJourney.value.id, 'Customising');
        await loadStageData();
    }
    catch (err) {
        errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
    }
}
async function requestQuote() {
    try {
        currentJourney.value = await api.advanceJourney(currentJourney.value.id, 'QuoteRequested');
        await loadStageData();
    }
    catch (err) {
        errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
    }
}
async function checkQuoteReady() {
    try {
        currentJourney.value = await api.getJourney(currentJourney.value.id);
    }
    catch (err) {
        errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
    }
}
async function completeJourney() {
    try {
        currentJourney.value = await api.advanceJourney(currentJourney.value.id, 'Completed');
    }
    catch (err) {
        errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
    }
}
async function startNewJourney() {
    currentJourney.value = null;
    await startJourney();
}
onMounted(async () => {
    const journeys = await api.getBuyerJourneys(DEMO_BUYER_ID);
    const active = journeys.find((j) => j.currentStage !== 'Completed' && j.currentStage !== 'Abandoned');
    if (active) {
        currentJourney.value = active;
        await loadStageData();
    }
    pastJourneys.value = journeys;
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
__VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
    ...{ class: "text-muted" },
});
/** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
if (!__VLS_ctx.currentJourney) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "card shadow-sm mb-4" },
    });
    /** @type {__VLS_StyleScopedClasses['card']} */ ;
    /** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
    /** @type {__VLS_StyleScopedClasses['mb-4']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "card-body text-center py-5" },
    });
    /** @type {__VLS_StyleScopedClasses['card-body']} */ ;
    /** @type {__VLS_StyleScopedClasses['text-center']} */ ;
    /** @type {__VLS_StyleScopedClasses['py-5']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.h4, __VLS_intrinsics.h4)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
        ...{ class: "text-muted" },
    });
    /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
        ...{ onClick: (__VLS_ctx.startJourney) },
        ...{ class: "btn btn-primary btn-lg" },
    });
    /** @type {__VLS_StyleScopedClasses['btn']} */ ;
    /** @type {__VLS_StyleScopedClasses['btn-primary']} */ ;
    /** @type {__VLS_StyleScopedClasses['btn-lg']} */ ;
    if (__VLS_ctx.pastJourneys && __VLS_ctx.pastJourneys.length > 0) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.h5, __VLS_intrinsics.h5)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "list-group mb-4" },
        });
        /** @type {__VLS_StyleScopedClasses['list-group']} */ ;
        /** @type {__VLS_StyleScopedClasses['mb-4']} */ ;
        for (const [j] of __VLS_vFor((__VLS_ctx.pastJourneys))) {
            __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
                key: (j.id),
                ...{ class: "list-group-item d-flex justify-content-between align-items-center" },
            });
            /** @type {__VLS_StyleScopedClasses['list-group-item']} */ ;
            /** @type {__VLS_StyleScopedClasses['d-flex']} */ ;
            /** @type {__VLS_StyleScopedClasses['justify-content-between']} */ ;
            /** @type {__VLS_StyleScopedClasses['align-items-center']} */ ;
            __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({});
            __VLS_asFunctionalElement1(__VLS_intrinsics.strong, __VLS_intrinsics.strong)({});
            (j.id.substring(0, 8));
            const __VLS_0 = StatusBadge;
            // @ts-ignore
            const __VLS_1 = __VLS_asFunctionalComponent1(__VLS_0, new __VLS_0({
                status: (j.currentStage),
            }));
            const __VLS_2 = __VLS_1({
                status: (j.currentStage),
            }, ...__VLS_functionalComponentArgsRest(__VLS_1));
            __VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({
                ...{ class: "text-muted" },
            });
            /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
            (new Date(j.startedUtc).toLocaleString());
            // @ts-ignore
            [currentJourney, startJourney, pastJourneys, pastJourneys, pastJourneys,];
        }
    }
}
else {
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "card shadow-sm mb-4" },
    });
    /** @type {__VLS_StyleScopedClasses['card']} */ ;
    /** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
    /** @type {__VLS_StyleScopedClasses['mb-4']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "card-body" },
    });
    /** @type {__VLS_StyleScopedClasses['card-body']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "d-flex justify-content-between align-items-center mb-3" },
    });
    /** @type {__VLS_StyleScopedClasses['d-flex']} */ ;
    /** @type {__VLS_StyleScopedClasses['justify-content-between']} */ ;
    /** @type {__VLS_StyleScopedClasses['align-items-center']} */ ;
    /** @type {__VLS_StyleScopedClasses['mb-3']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.h5, __VLS_intrinsics.h5)({
        ...{ class: "mb-0" },
    });
    /** @type {__VLS_StyleScopedClasses['mb-0']} */ ;
    const __VLS_5 = StatusBadge;
    // @ts-ignore
    const __VLS_6 = __VLS_asFunctionalComponent1(__VLS_5, new __VLS_5({
        status: (__VLS_ctx.currentJourney.currentStage),
    }));
    const __VLS_7 = __VLS_6({
        status: (__VLS_ctx.currentJourney.currentStage),
    }, ...__VLS_functionalComponentArgsRest(__VLS_6));
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "progress mb-3" },
        ...{ style: {} },
    });
    /** @type {__VLS_StyleScopedClasses['progress']} */ ;
    /** @type {__VLS_StyleScopedClasses['mb-3']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "progress-bar bg-success" },
        ...{ style: ({ width: __VLS_ctx.getProgressPercent() + '%' }) },
    });
    /** @type {__VLS_StyleScopedClasses['progress-bar']} */ ;
    /** @type {__VLS_StyleScopedClasses['bg-success']} */ ;
    (__VLS_ctx.currentJourney.currentStage);
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "row text-center" },
    });
    /** @type {__VLS_StyleScopedClasses['row']} */ ;
    /** @type {__VLS_StyleScopedClasses['text-center']} */ ;
    for (const [stage] of __VLS_vFor((__VLS_ctx.journeyStages))) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            key: (stage),
            ...{ class: "col" },
            ...{ class: ({
                    'text-success': __VLS_ctx.journeyStages.indexOf(__VLS_ctx.currentJourney.currentStage) >= __VLS_ctx.journeyStages.indexOf(stage),
                    'text-muted': __VLS_ctx.journeyStages.indexOf(__VLS_ctx.currentJourney.currentStage) < __VLS_ctx.journeyStages.indexOf(stage),
                    'fw-bold': __VLS_ctx.currentJourney.currentStage === stage,
                }) },
        });
        /** @type {__VLS_StyleScopedClasses['col']} */ ;
        /** @type {__VLS_StyleScopedClasses['text-success']} */ ;
        /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
        /** @type {__VLS_StyleScopedClasses['fw-bold']} */ ;
        (__VLS_ctx.journeyStages.indexOf(__VLS_ctx.currentJourney.currentStage) >= __VLS_ctx.journeyStages.indexOf(stage) ? '✅' : '⬜');
        (stage);
        // @ts-ignore
        [currentJourney, currentJourney, currentJourney, currentJourney, currentJourney, currentJourney, getProgressPercent, journeyStages, journeyStages, journeyStages, journeyStages, journeyStages, journeyStages, journeyStages,];
    }
    if (__VLS_ctx.currentJourney.currentStage === 'Browsing') {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card shadow-sm mb-4" },
        });
        /** @type {__VLS_StyleScopedClasses['card']} */ ;
        /** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
        /** @type {__VLS_StyleScopedClasses['mb-4']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card-body" },
        });
        /** @type {__VLS_StyleScopedClasses['card-body']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.h5, __VLS_intrinsics.h5)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({});
        if (__VLS_ctx.availableModels) {
            __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
                ...{ class: "row g-3" },
            });
            /** @type {__VLS_StyleScopedClasses['row']} */ ;
            /** @type {__VLS_StyleScopedClasses['g-3']} */ ;
            for (const [model] of __VLS_vFor((__VLS_ctx.availableModels.slice(0, 6)))) {
                __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
                    ...{ class: "col-md-4" },
                    key: (model.id),
                });
                /** @type {__VLS_StyleScopedClasses['col-md-4']} */ ;
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
                (model.floorAreaSqm.toFixed(0));
                __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
                    ...{ class: "card-footer" },
                });
                /** @type {__VLS_StyleScopedClasses['card-footer']} */ ;
                __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
                    ...{ onClick: (...[$event]) => {
                            if (!!(!__VLS_ctx.currentJourney))
                                return;
                            if (!(__VLS_ctx.currentJourney.currentStage === 'Browsing'))
                                return;
                            if (!(__VLS_ctx.availableModels))
                                return;
                            __VLS_ctx.selectDesign(model.id);
                            // @ts-ignore
                            [currentJourney, availableModels, availableModels, selectDesign,];
                        } },
                    ...{ class: "btn btn-sm btn-primary w-100" },
                });
                /** @type {__VLS_StyleScopedClasses['btn']} */ ;
                /** @type {__VLS_StyleScopedClasses['btn-sm']} */ ;
                /** @type {__VLS_StyleScopedClasses['btn-primary']} */ ;
                /** @type {__VLS_StyleScopedClasses['w-100']} */ ;
                // @ts-ignore
                [];
            }
        }
    }
    if (__VLS_ctx.currentJourney.currentStage === 'DesignSelected') {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card shadow-sm mb-4" },
        });
        /** @type {__VLS_StyleScopedClasses['card']} */ ;
        /** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
        /** @type {__VLS_StyleScopedClasses['mb-4']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card-body" },
        });
        /** @type {__VLS_StyleScopedClasses['card-body']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.h5, __VLS_intrinsics.h5)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({});
        if (__VLS_ctx.availableLand) {
            __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
                ...{ class: "list-group" },
            });
            /** @type {__VLS_StyleScopedClasses['list-group']} */ ;
            for (const [block] of __VLS_vFor((__VLS_ctx.availableLand.slice(0, 5)))) {
                __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
                    ...{ onClick: (...[$event]) => {
                            if (!!(!__VLS_ctx.currentJourney))
                                return;
                            if (!(__VLS_ctx.currentJourney.currentStage === 'DesignSelected'))
                                return;
                            if (!(__VLS_ctx.availableLand))
                                return;
                            __VLS_ctx.selectLand(block.id);
                            // @ts-ignore
                            [currentJourney, availableLand, availableLand, selectLand,];
                        } },
                    key: (block.id),
                    ...{ class: "list-group-item list-group-item-action" },
                });
                /** @type {__VLS_StyleScopedClasses['list-group-item']} */ ;
                /** @type {__VLS_StyleScopedClasses['list-group-item-action']} */ ;
                __VLS_asFunctionalElement1(__VLS_intrinsics.strong, __VLS_intrinsics.strong)({});
                (block.address);
                (block.suburb);
                (block.state);
                (block.areaSqm.toFixed(0));
                (block.zoning);
                // @ts-ignore
                [];
            }
        }
    }
    if (__VLS_ctx.currentJourney.currentStage === 'PlacedOnLand') {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card shadow-sm mb-4" },
        });
        /** @type {__VLS_StyleScopedClasses['card']} */ ;
        /** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
        /** @type {__VLS_StyleScopedClasses['mb-4']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card-body" },
        });
        /** @type {__VLS_StyleScopedClasses['card-body']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.h5, __VLS_intrinsics.h5)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
            ...{ onClick: (__VLS_ctx.moveToCustomising) },
            ...{ class: "btn btn-primary" },
        });
        /** @type {__VLS_StyleScopedClasses['btn']} */ ;
        /** @type {__VLS_StyleScopedClasses['btn-primary']} */ ;
    }
    if (__VLS_ctx.currentJourney.currentStage === 'Customising') {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card shadow-sm mb-4" },
        });
        /** @type {__VLS_StyleScopedClasses['card']} */ ;
        /** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
        /** @type {__VLS_StyleScopedClasses['mb-4']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card-body" },
        });
        /** @type {__VLS_StyleScopedClasses['card-body']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.h5, __VLS_intrinsics.h5)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
            ...{ onClick: (__VLS_ctx.requestQuote) },
            ...{ class: "btn btn-primary" },
        });
        /** @type {__VLS_StyleScopedClasses['btn']} */ ;
        /** @type {__VLS_StyleScopedClasses['btn-primary']} */ ;
    }
    if (__VLS_ctx.currentJourney.currentStage === 'QuoteRequested') {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card shadow-sm mb-4" },
        });
        /** @type {__VLS_StyleScopedClasses['card']} */ ;
        /** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
        /** @type {__VLS_StyleScopedClasses['mb-4']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card-body text-center" },
        });
        /** @type {__VLS_StyleScopedClasses['card-body']} */ ;
        /** @type {__VLS_StyleScopedClasses['text-center']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.h5, __VLS_intrinsics.h5)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
            ...{ class: "text-muted" },
        });
        /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
            ...{ onClick: (__VLS_ctx.checkQuoteReady) },
            ...{ class: "btn btn-outline-primary" },
        });
        /** @type {__VLS_StyleScopedClasses['btn']} */ ;
        /** @type {__VLS_StyleScopedClasses['btn-outline-primary']} */ ;
    }
    if (__VLS_ctx.currentJourney.currentStage === 'QuoteReceived') {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card shadow-sm mb-4" },
        });
        /** @type {__VLS_StyleScopedClasses['card']} */ ;
        /** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
        /** @type {__VLS_StyleScopedClasses['mb-4']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card-body" },
        });
        /** @type {__VLS_StyleScopedClasses['card-body']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.h5, __VLS_intrinsics.h5)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
            ...{ onClick: (__VLS_ctx.completeJourney) },
            ...{ class: "btn btn-success" },
        });
        /** @type {__VLS_StyleScopedClasses['btn']} */ ;
        /** @type {__VLS_StyleScopedClasses['btn-success']} */ ;
    }
    if (__VLS_ctx.currentJourney.currentStage === 'Completed' || __VLS_ctx.currentJourney.currentStage === 'Referred') {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "alert alert-success text-center" },
        });
        /** @type {__VLS_StyleScopedClasses['alert']} */ ;
        /** @type {__VLS_StyleScopedClasses['alert-success']} */ ;
        /** @type {__VLS_StyleScopedClasses['text-center']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.h4, __VLS_intrinsics.h4)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
            ...{ onClick: (__VLS_ctx.startNewJourney) },
            ...{ class: "btn btn-outline-primary" },
        });
        /** @type {__VLS_StyleScopedClasses['btn']} */ ;
        /** @type {__VLS_StyleScopedClasses['btn-outline-primary']} */ ;
    }
    const __VLS_10 = ErrorAlert;
    // @ts-ignore
    const __VLS_11 = __VLS_asFunctionalComponent1(__VLS_10, new __VLS_10({
        message: (__VLS_ctx.errorMessage),
    }));
    const __VLS_12 = __VLS_11({
        message: (__VLS_ctx.errorMessage),
    }, ...__VLS_functionalComponentArgsRest(__VLS_11));
}
// @ts-ignore
[currentJourney, currentJourney, currentJourney, currentJourney, currentJourney, currentJourney, moveToCustomising, requestQuote, checkQuoteReady, completeJourney, startNewJourney, errorMessage,];
const __VLS_export = (await import('vue')).defineComponent({});
export default {};

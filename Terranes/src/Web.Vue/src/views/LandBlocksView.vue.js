/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/template-helpers.d.ts" />
/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/props-fallback.d.ts" />
import { ref, onMounted, watch } from 'vue';
import { api } from '../api/client';
import LoadingSpinner from '../components/LoadingSpinner.vue';
import DetailModal from '../components/DetailModal.vue';
import ErrorAlert from '../components/ErrorAlert.vue';
const blocks = ref(null);
const searchSuburb = ref('');
const searchState = ref('');
const selectedBlock = ref(null);
const availableModels = ref(null);
const placementResult = ref(null);
const placementError = ref(null);
async function search() {
    blocks.value = await api.getLandBlocks({
        suburb: searchSuburb.value || undefined,
        state: searchState.value || undefined,
    });
}
async function selectBlock(block) {
    selectedBlock.value = block;
    placementResult.value = null;
    placementError.value = null;
    availableModels.value = await api.getHomeModels();
}
async function testFit(model) {
    try {
        placementError.value = null;
        placementResult.value = await api.createSitePlacement(selectedBlock.value.id, model.id);
    }
    catch (err) {
        placementError.value = err instanceof Error ? err.message : 'Unknown error';
        placementResult.value = null;
    }
}
function closeModal() {
    selectedBlock.value = null;
    availableModels.value = null;
    placementResult.value = null;
    placementError.value = null;
}
onMounted(search);
watch([searchSuburb, searchState], search);
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
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "row mb-3" },
});
/** @type {__VLS_StyleScopedClasses['row']} */ ;
/** @type {__VLS_StyleScopedClasses['mb-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-4" },
});
/** @type {__VLS_StyleScopedClasses['col-md-4']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.input)({
    type: "text",
    ...{ class: "form-control" },
    placeholder: "Search by suburb...",
    value: (__VLS_ctx.searchSuburb),
});
/** @type {__VLS_StyleScopedClasses['form-control']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-3" },
});
/** @type {__VLS_StyleScopedClasses['col-md-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.input)({
    type: "text",
    ...{ class: "form-control" },
    placeholder: "State (e.g. NSW)",
    value: (__VLS_ctx.searchState),
});
/** @type {__VLS_StyleScopedClasses['form-control']} */ ;
if (__VLS_ctx.blocks === null) {
    const __VLS_0 = LoadingSpinner;
    // @ts-ignore
    const __VLS_1 = __VLS_asFunctionalComponent1(__VLS_0, new __VLS_0({
        message: "Loading land blocks...",
    }));
    const __VLS_2 = __VLS_1({
        message: "Loading land blocks...",
    }, ...__VLS_functionalComponentArgsRest(__VLS_1));
}
else if (__VLS_ctx.blocks.length === 0) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "alert alert-info" },
    });
    /** @type {__VLS_StyleScopedClasses['alert']} */ ;
    /** @type {__VLS_StyleScopedClasses['alert-info']} */ ;
}
else {
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "table-responsive" },
    });
    /** @type {__VLS_StyleScopedClasses['table-responsive']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.table, __VLS_intrinsics.table)({
        ...{ class: "table table-hover" },
    });
    /** @type {__VLS_StyleScopedClasses['table']} */ ;
    /** @type {__VLS_StyleScopedClasses['table-hover']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.thead, __VLS_intrinsics.thead)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.tbody, __VLS_intrinsics.tbody)({});
    for (const [block] of __VLS_vFor((__VLS_ctx.blocks))) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({
            key: (block.id),
        });
        __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
        (block.address);
        __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
        (block.suburb);
        __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
        (block.state);
        __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
        (block.areaSqm.toFixed(0));
        __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
        (block.frontageMetre.toFixed(1));
        __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
        (block.depthMetre.toFixed(1));
        __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
            ...{ class: "badge bg-secondary" },
        });
        /** @type {__VLS_StyleScopedClasses['badge']} */ ;
        /** @type {__VLS_StyleScopedClasses['bg-secondary']} */ ;
        (block.zoning);
        __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
            ...{ onClick: (...[$event]) => {
                    if (!!(__VLS_ctx.blocks === null))
                        return;
                    if (!!(__VLS_ctx.blocks.length === 0))
                        return;
                    __VLS_ctx.selectBlock(block);
                    // @ts-ignore
                    [searchSuburb, searchState, blocks, blocks, blocks, selectBlock,];
                } },
            ...{ class: "btn btn-sm btn-outline-primary" },
        });
        /** @type {__VLS_StyleScopedClasses['btn']} */ ;
        /** @type {__VLS_StyleScopedClasses['btn-sm']} */ ;
        /** @type {__VLS_StyleScopedClasses['btn-outline-primary']} */ ;
        // @ts-ignore
        [];
    }
}
const __VLS_5 = DetailModal || DetailModal;
// @ts-ignore
const __VLS_6 = __VLS_asFunctionalComponent1(__VLS_5, new __VLS_5({
    ...{ 'onClose': {} },
    show: (!!__VLS_ctx.selectedBlock),
    title: (__VLS_ctx.selectedBlock ? 'Test-Fit on ' + __VLS_ctx.selectedBlock.address : ''),
}));
const __VLS_7 = __VLS_6({
    ...{ 'onClose': {} },
    show: (!!__VLS_ctx.selectedBlock),
    title: (__VLS_ctx.selectedBlock ? 'Test-Fit on ' + __VLS_ctx.selectedBlock.address : ''),
}, ...__VLS_functionalComponentArgsRest(__VLS_6));
let __VLS_10;
const __VLS_11 = ({ close: {} },
    { onClose: (__VLS_ctx.closeModal) });
const { default: __VLS_12 } = __VLS_8.slots;
if (__VLS_ctx.selectedBlock) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "row mb-3" },
    });
    /** @type {__VLS_StyleScopedClasses['row']} */ ;
    /** @type {__VLS_StyleScopedClasses['mb-3']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "col" },
    });
    /** @type {__VLS_StyleScopedClasses['col']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.table, __VLS_intrinsics.table)({
        ...{ class: "table table-sm" },
    });
    /** @type {__VLS_StyleScopedClasses['table']} */ ;
    /** @type {__VLS_StyleScopedClasses['table-sm']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.tbody, __VLS_intrinsics.tbody)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedBlock.address);
    (__VLS_ctx.selectedBlock.suburb);
    (__VLS_ctx.selectedBlock.state);
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedBlock.areaSqm.toFixed(0));
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedBlock.frontageMetre.toFixed(1));
    (__VLS_ctx.selectedBlock.depthMetre.toFixed(1));
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.selectedBlock.zoning);
    __VLS_asFunctionalElement1(__VLS_intrinsics.h6, __VLS_intrinsics.h6)({});
    if (__VLS_ctx.availableModels === null) {
        const __VLS_13 = LoadingSpinner;
        // @ts-ignore
        const __VLS_14 = __VLS_asFunctionalComponent1(__VLS_13, new __VLS_13({
            message: "Loading designs...",
        }));
        const __VLS_15 = __VLS_14({
            message: "Loading designs...",
        }, ...__VLS_functionalComponentArgsRest(__VLS_14));
    }
    else if (__VLS_ctx.availableModels.length === 0) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
            ...{ class: "text-muted" },
        });
        /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
    }
    else {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "list-group" },
        });
        /** @type {__VLS_StyleScopedClasses['list-group']} */ ;
        for (const [model] of __VLS_vFor((__VLS_ctx.availableModels))) {
            __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
                ...{ onClick: (...[$event]) => {
                        if (!(__VLS_ctx.selectedBlock))
                            return;
                        if (!!(__VLS_ctx.availableModels === null))
                            return;
                        if (!!(__VLS_ctx.availableModels.length === 0))
                            return;
                        __VLS_ctx.testFit(model);
                        // @ts-ignore
                        [selectedBlock, selectedBlock, selectedBlock, selectedBlock, selectedBlock, selectedBlock, selectedBlock, selectedBlock, selectedBlock, selectedBlock, selectedBlock, closeModal, availableModels, availableModels, availableModels, testFit,];
                    } },
                key: (model.id),
                ...{ class: "list-group-item list-group-item-action d-flex justify-content-between align-items-center" },
            });
            /** @type {__VLS_StyleScopedClasses['list-group-item']} */ ;
            /** @type {__VLS_StyleScopedClasses['list-group-item-action']} */ ;
            /** @type {__VLS_StyleScopedClasses['d-flex']} */ ;
            /** @type {__VLS_StyleScopedClasses['justify-content-between']} */ ;
            /** @type {__VLS_StyleScopedClasses['align-items-center']} */ ;
            __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({});
            __VLS_asFunctionalElement1(__VLS_intrinsics.strong, __VLS_intrinsics.strong)({});
            (model.name);
            (model.bedrooms);
            (model.bathrooms);
            (model.floorAreaSqm.toFixed(0));
            __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
                ...{ class: "badge bg-primary" },
            });
            /** @type {__VLS_StyleScopedClasses['badge']} */ ;
            /** @type {__VLS_StyleScopedClasses['bg-primary']} */ ;
            // @ts-ignore
            [];
        }
    }
    if (__VLS_ctx.placementResult) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "alert alert-success mt-3" },
        });
        /** @type {__VLS_StyleScopedClasses['alert']} */ ;
        /** @type {__VLS_StyleScopedClasses['alert-success']} */ ;
        /** @type {__VLS_StyleScopedClasses['mt-3']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.strong, __VLS_intrinsics.strong)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.br)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.code, __VLS_intrinsics.code)({});
        (__VLS_ctx.placementResult.id);
        __VLS_asFunctionalElement1(__VLS_intrinsics.br)({});
        (__VLS_ctx.placementResult.offsetX.toFixed(1));
        (__VLS_ctx.placementResult.offsetY.toFixed(1));
        (__VLS_ctx.placementResult.rotationDegrees);
        (__VLS_ctx.placementResult.scaleFactor);
    }
    const __VLS_18 = ErrorAlert;
    // @ts-ignore
    const __VLS_19 = __VLS_asFunctionalComponent1(__VLS_18, new __VLS_18({
        message: (__VLS_ctx.placementError),
    }));
    const __VLS_20 = __VLS_19({
        message: (__VLS_ctx.placementError),
    }, ...__VLS_functionalComponentArgsRest(__VLS_19));
}
// @ts-ignore
[placementResult, placementResult, placementResult, placementResult, placementResult, placementResult, placementError,];
var __VLS_8;
var __VLS_9;
// @ts-ignore
[];
const __VLS_export = (await import('vue')).defineComponent({});
export default {};

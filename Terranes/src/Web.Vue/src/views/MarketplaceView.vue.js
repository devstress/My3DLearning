/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/template-helpers.d.ts" />
/// <reference types="../../../../../../../../.npm/_npx/2db181330ea4b15b/node_modules/@vue/language-core/types/props-fallback.d.ts" />
import { ref, onMounted, watch } from 'vue';
import { api } from '../api/client';
import LoadingSpinner from '../components/LoadingSpinner.vue';
import DetailModal from '../components/DetailModal.vue';
import StatusBadge from '../components/StatusBadge.vue';
const listings = ref(null);
const searchSuburb = ref('');
const maxPrice = ref(undefined);
const selectedStatus = ref('');
const selectedListing = ref(null);
const statuses = ['Active', 'Draft', 'UnderOffer', 'Sold', 'Withdrawn'];
function formatPrice(price) {
    if (price == null)
        return 'Price on Application';
    return `$${price.toLocaleString('en-AU', { maximumFractionDigits: 0 })}`;
}
async function search() {
    listings.value = await api.getListings({
        suburb: searchSuburb.value || undefined,
        maxPriceAud: maxPrice.value,
        status: selectedStatus.value || undefined,
    });
}
function viewListing(listing) {
    selectedListing.value = listing;
}
function closeModal() {
    selectedListing.value = null;
}
onMounted(search);
watch([searchSuburb, maxPrice, selectedStatus], search);
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
    ...{ class: "col-md-3" },
});
/** @type {__VLS_StyleScopedClasses['col-md-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.input)({
    type: "text",
    ...{ class: "form-control" },
    placeholder: "Suburb...",
    value: (__VLS_ctx.searchSuburb),
});
/** @type {__VLS_StyleScopedClasses['form-control']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-3" },
});
/** @type {__VLS_StyleScopedClasses['col-md-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.input)({
    type: "number",
    ...{ class: "form-control" },
    placeholder: "Max price ($)",
});
(__VLS_ctx.maxPrice);
/** @type {__VLS_StyleScopedClasses['form-control']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "col-md-3" },
});
/** @type {__VLS_StyleScopedClasses['col-md-3']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.select, __VLS_intrinsics.select)({
    ...{ class: "form-select" },
    value: (__VLS_ctx.selectedStatus),
});
/** @type {__VLS_StyleScopedClasses['form-select']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.option, __VLS_intrinsics.option)({
    value: "",
});
for (const [s] of __VLS_vFor((__VLS_ctx.statuses))) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.option, __VLS_intrinsics.option)({
        key: (s),
        value: (s),
    });
    (s);
    // @ts-ignore
    [searchSuburb, maxPrice, selectedStatus, statuses,];
}
if (__VLS_ctx.listings === null) {
    const __VLS_0 = LoadingSpinner;
    // @ts-ignore
    const __VLS_1 = __VLS_asFunctionalComponent1(__VLS_0, new __VLS_0({
        message: "Loading listings...",
    }));
    const __VLS_2 = __VLS_1({
        message: "Loading listings...",
    }, ...__VLS_functionalComponentArgsRest(__VLS_1));
}
else if (__VLS_ctx.listings.length === 0) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "alert alert-info" },
    });
    /** @type {__VLS_StyleScopedClasses['alert']} */ ;
    /** @type {__VLS_StyleScopedClasses['alert-info']} */ ;
}
else {
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "row g-4" },
    });
    /** @type {__VLS_StyleScopedClasses['row']} */ ;
    /** @type {__VLS_StyleScopedClasses['g-4']} */ ;
    for (const [listing] of __VLS_vFor((__VLS_ctx.listings))) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "col-md-6" },
            key: (listing.id),
        });
        /** @type {__VLS_StyleScopedClasses['col-md-6']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card h-100 shadow-sm" },
        });
        /** @type {__VLS_StyleScopedClasses['card']} */ ;
        /** @type {__VLS_StyleScopedClasses['h-100']} */ ;
        /** @type {__VLS_StyleScopedClasses['shadow-sm']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card-body" },
        });
        /** @type {__VLS_StyleScopedClasses['card-body']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "d-flex justify-content-between align-items-start" },
        });
        /** @type {__VLS_StyleScopedClasses['d-flex']} */ ;
        /** @type {__VLS_StyleScopedClasses['justify-content-between']} */ ;
        /** @type {__VLS_StyleScopedClasses['align-items-start']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.h5, __VLS_intrinsics.h5)({
            ...{ class: "card-title" },
        });
        /** @type {__VLS_StyleScopedClasses['card-title']} */ ;
        (listing.title);
        const __VLS_5 = StatusBadge;
        // @ts-ignore
        const __VLS_6 = __VLS_asFunctionalComponent1(__VLS_5, new __VLS_5({
            status: (listing.status),
        }));
        const __VLS_7 = __VLS_6({
            status: (listing.status),
        }, ...__VLS_functionalComponentArgsRest(__VLS_6));
        __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
            ...{ class: "card-text text-muted" },
        });
        /** @type {__VLS_StyleScopedClasses['card-text']} */ ;
        /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
        (listing.description);
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "d-flex justify-content-between" },
        });
        /** @type {__VLS_StyleScopedClasses['d-flex']} */ ;
        /** @type {__VLS_StyleScopedClasses['justify-content-between']} */ ;
        if (listing.askingPriceAud != null) {
            __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
                ...{ class: "h5 text-success" },
            });
            /** @type {__VLS_StyleScopedClasses['h5']} */ ;
            /** @type {__VLS_StyleScopedClasses['text-success']} */ ;
            (__VLS_ctx.formatPrice(listing.askingPriceAud));
        }
        else {
            __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
                ...{ class: "text-muted" },
            });
            /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
        }
        __VLS_asFunctionalElement1(__VLS_intrinsics.small, __VLS_intrinsics.small)({
            ...{ class: "text-muted" },
        });
        /** @type {__VLS_StyleScopedClasses['text-muted']} */ ;
        (new Date(listing.listedUtc).toLocaleDateString());
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...{ class: "card-footer" },
        });
        /** @type {__VLS_StyleScopedClasses['card-footer']} */ ;
        __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
            ...{ onClick: (...[$event]) => {
                    if (!!(__VLS_ctx.listings === null))
                        return;
                    if (!!(__VLS_ctx.listings.length === 0))
                        return;
                    __VLS_ctx.viewListing(listing);
                    // @ts-ignore
                    [listings, listings, listings, formatPrice, viewListing,];
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
const __VLS_10 = DetailModal || DetailModal;
// @ts-ignore
const __VLS_11 = __VLS_asFunctionalComponent1(__VLS_10, new __VLS_10({
    ...{ 'onClose': {} },
    show: (!!__VLS_ctx.selectedListing),
    title: (__VLS_ctx.selectedListing?.title ?? ''),
}));
const __VLS_12 = __VLS_11({
    ...{ 'onClose': {} },
    show: (!!__VLS_ctx.selectedListing),
    title: (__VLS_ctx.selectedListing?.title ?? ''),
}, ...__VLS_functionalComponentArgsRest(__VLS_11));
let __VLS_15;
const __VLS_16 = ({ close: {} },
    { onClose: (__VLS_ctx.closeModal) });
const { default: __VLS_17 } = __VLS_13.slots;
if (__VLS_ctx.selectedListing) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({});
    (__VLS_ctx.selectedListing.description);
    __VLS_asFunctionalElement1(__VLS_intrinsics.table, __VLS_intrinsics.table)({
        ...{ class: "table table-sm" },
    });
    /** @type {__VLS_StyleScopedClasses['table']} */ ;
    /** @type {__VLS_StyleScopedClasses['table-sm']} */ ;
    __VLS_asFunctionalElement1(__VLS_intrinsics.tbody, __VLS_intrinsics.tbody)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    const __VLS_18 = StatusBadge;
    // @ts-ignore
    const __VLS_19 = __VLS_asFunctionalComponent1(__VLS_18, new __VLS_18({
        status: (__VLS_ctx.selectedListing.status),
    }));
    const __VLS_20 = __VLS_19({
        status: (__VLS_ctx.selectedListing.status),
    }, ...__VLS_functionalComponentArgsRest(__VLS_19));
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (__VLS_ctx.formatPrice(__VLS_ctx.selectedListing.askingPriceAud));
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.code, __VLS_intrinsics.code)({});
    (__VLS_ctx.selectedListing.homeModelId);
    if (__VLS_ctx.selectedListing.landBlockId) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
        __VLS_asFunctionalElement1(__VLS_intrinsics.code, __VLS_intrinsics.code)({});
        (__VLS_ctx.selectedListing.landBlockId);
    }
    __VLS_asFunctionalElement1(__VLS_intrinsics.tr, __VLS_intrinsics.tr)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.th, __VLS_intrinsics.th)({});
    __VLS_asFunctionalElement1(__VLS_intrinsics.td, __VLS_intrinsics.td)({});
    (new Date(__VLS_ctx.selectedListing.listedUtc).toLocaleString());
}
// @ts-ignore
[formatPrice, selectedListing, selectedListing, selectedListing, selectedListing, selectedListing, selectedListing, selectedListing, selectedListing, selectedListing, selectedListing, closeModal,];
var __VLS_13;
var __VLS_14;
// @ts-ignore
[];
const __VLS_export = (await import('vue')).defineComponent({});
export default {};

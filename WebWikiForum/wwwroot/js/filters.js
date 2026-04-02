/**
 * VTuber Wiki - Client-side Filter System
 * Handles tab switching, search filtering, dropdown filters, and tag-based filtering.
 */
document.addEventListener('DOMContentLoaded', function () {

    // ===== 1. Tab Filter (RecentChanges, WikiForum, FanTools) =====
    document.querySelectorAll('[data-filter-group]').forEach(function (group) {
        const buttons = group.querySelectorAll('[data-filter]');
        const targetId = group.dataset.filterGroup;
        const items = document.querySelectorAll('[data-filter-target="' + targetId + '"]');

        buttons.forEach(function (btn) {
            btn.addEventListener('click', function () {
                // Update active state
                buttons.forEach(function (b) {
                    b.classList.remove('bg-primary', 'text-white', 'border-primary');
                    b.classList.add('text-slate-600', 'dark:text-slate-300');
                    // For tab-style (border-bottom)
                    if (b.classList.contains('border-b-[3px]')) {
                        b.classList.remove('border-primary', 'text-slate-900', 'dark:text-slate-100');
                        b.classList.add('border-transparent', 'text-slate-500');
                    }
                });
                btn.classList.add('bg-primary', 'text-white');
                btn.classList.remove('text-slate-600', 'dark:text-slate-300');
                // For tab-style
                if (btn.classList.contains('border-b-[3px]')) {
                    btn.classList.add('border-primary', 'text-slate-900', 'dark:text-slate-100');
                    btn.classList.remove('border-transparent', 'text-slate-500');
                }

                const filterValue = btn.dataset.filter;

                items.forEach(function (item) {
                    if (filterValue === 'all' || item.dataset.category === filterValue) {
                        item.style.display = '';
                        item.style.opacity = '1';
                    } else {
                        item.style.display = 'none';
                        item.style.opacity = '0';
                    }
                });
            });
        });
    });

    // ===== 2. Search Filter =====
    document.querySelectorAll('[data-search-input]').forEach(function (input) {
        const targetId = input.dataset.searchInput;

        input.addEventListener('input', function () {
            const query = input.value.toLowerCase().trim();
            const items = document.querySelectorAll('[data-search-target="' + targetId + '"]');

            items.forEach(function (item) {
                const text = item.textContent.toLowerCase();
                if (query === '' || text.includes(query)) {
                    item.style.display = '';
                } else {
                    item.style.display = 'none';
                }
            });
        });
    });

    // ===== 3. Dropdown Filter (Agencies, Independent) =====
    document.querySelectorAll('[data-dropdown-toggle]').forEach(function (btn) {
        const dropdownId = btn.dataset.dropdownToggle;
        const dropdown = document.getElementById(dropdownId);
        if (!dropdown) return;

        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            // Close all other dropdowns
            document.querySelectorAll('.filter-dropdown.is-open').forEach(function (d) {
                if (d !== dropdown) {
                    d.classList.remove('is-open');
                    d.style.opacity = '0';
                    d.style.visibility = 'hidden';
                }
            });
            // Toggle this dropdown
            const isOpen = dropdown.classList.contains('is-open');
            if (isOpen) {
                dropdown.classList.remove('is-open');
                dropdown.style.opacity = '0';
                dropdown.style.visibility = 'hidden';
            } else {
                dropdown.classList.add('is-open');
                dropdown.style.opacity = '1';
                dropdown.style.visibility = 'visible';
            }
        });

        // Handle dropdown item selection
        dropdown.querySelectorAll('[data-filter-value]').forEach(function (item) {
            item.addEventListener('click', function (e) {
                e.preventDefault();
                const val = item.dataset.filterValue;
                const label = btn.querySelector('.filter-label');
                if (label) {
                    label.textContent = item.textContent.trim();
                }
                btn.classList.add('border-primary', 'bg-primary/5');

                // Close dropdown
                dropdown.classList.remove('is-open');
                dropdown.style.opacity = '0';
                dropdown.style.visibility = 'hidden';

                // Trigger filtering
                applyCardFilters();
            });
        });
    });

    // Close dropdowns on outside click
    document.addEventListener('click', function () {
        document.querySelectorAll('.filter-dropdown.is-open').forEach(function (d) {
            d.classList.remove('is-open');
            d.style.opacity = '0';
            d.style.visibility = 'hidden';
        });
    });

    // ===== 4. Tag Filter (Independent) =====
    document.querySelectorAll('[data-tag-filter]').forEach(function (tag) {
        tag.addEventListener('click', function (e) {
            e.preventDefault();
            const active = tag.classList.contains('bg-primary');
            // Reset all tags
            document.querySelectorAll('[data-tag-filter]').forEach(function (t) {
                t.classList.remove('bg-primary', 'text-white', 'border-primary');
                t.classList.add('bg-white', 'dark:bg-slate-800', 'border-primary/5');
            });
            if (!active) {
                tag.classList.add('bg-primary', 'text-white', 'border-primary');
                tag.classList.remove('bg-white', 'dark:bg-slate-800', 'border-primary/5');
            }
            applyCardFilters();
        });
    });

    function applyCardFilters() {
        // This is a placeholder for applying combined filters
        // In a real app, this would filter cards based on dropdown + tag + search
        const cards = document.querySelectorAll('[data-search-target]');
        cards.forEach(function (card) {
            card.style.display = '';
        });
    }

    // ===== 5. Category Tab System (FanTools) =====
    document.querySelectorAll('[data-category-tab]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const category = btn.dataset.categoryTab;
            const allBtns = document.querySelectorAll('[data-category-tab]');
            const allSections = document.querySelectorAll('[data-tool-section]');

            // Update button states
            allBtns.forEach(function (b) {
                b.classList.remove('bg-primary', 'text-white');
                b.classList.add('bg-white', 'dark:bg-slate-800', 'border-primary/20');
            });
            btn.classList.add('bg-primary', 'text-white');
            btn.classList.remove('bg-white', 'dark:bg-slate-800', 'border-primary/20');

            // Show/hide sections
            allSections.forEach(function (section) {
                if (category === 'all' || section.dataset.toolSection === category) {
                    section.style.display = '';
                } else {
                    section.style.display = 'none';
                }
            });
        });
    });

    // ===== 6. Forum Tab System (WikiForum) =====
    document.querySelectorAll('[data-forum-tab]').forEach(function (tab) {
        tab.addEventListener('click', function (e) {
            e.preventDefault();
            const allTabs = document.querySelectorAll('[data-forum-tab]');
            allTabs.forEach(function (t) {
                t.classList.remove('border-primary', 'text-slate-900', 'dark:text-slate-100');
                t.classList.add('border-transparent', 'text-slate-500');
            });
            tab.classList.add('border-primary', 'text-slate-900', 'dark:text-slate-100');
            tab.classList.remove('border-transparent', 'text-slate-500');
        });
    });
});

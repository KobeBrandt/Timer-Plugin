let currentConfig = null;
let editingTimerIndex = null;

// Define Web Components
class TimerCard extends HTMLElement {
    constructor() {
        super();
    }

    connectedCallback() {
        const timer = JSON.parse(this.getAttribute('data-timer'));
        const index = parseInt(this.getAttribute('data-index'));

        this.className = 'group-card';
        this.dataset.index = index;

        // Clone template
        const template = document.getElementById('timer-card-template');
        const clone = template.content.cloneNode(true);

        // Populate timer name
        const timerName = clone.querySelector('.group-name');
        timerName.textContent = timer.Name;

        // Populate timer info
        const infoElement = clone.querySelector('.timer-info');
        const timeDisplay = this.formatTime(timer.Hours, timer.Minutes, timer.Seconds);
        infoElement.textContent = `${timeDisplay} • ${timer.Haptic}`;
        infoElement.style.padding = '0.75rem';
        infoElement.style.color = 'var(--text-secondary)';

        // Populate toggle
        const toggleInput = clone.querySelector('.timer-toggle');
        toggleInput.checked = timer.IsActive;
        toggleInput.dataset.index = index;

        // Set delete button data
        const deleteBtn = clone.querySelector('.btn-delete');
        deleteBtn.dataset.index = index;

        this.appendChild(clone);
        this.attachEventListeners();
    }

    formatTime(hours, minutes, seconds) {
        const parts = [];
        if (hours > 0) parts.push(`${hours}h`);
        if (minutes > 0) parts.push(`${minutes}m`);
        if (seconds > 0) parts.push(`${seconds}s`);
        return parts.join(' ') || '0s';
    }

    attachEventListeners() {
        // Card click to open modal (except toggle and delete button)
        this.addEventListener('click', (e) => {
            if (!e.target.closest('.toggle') && !e.target.closest('.btn-delete')) {
                const index = parseInt(this.dataset.index);
                openModal(index);
            }
        });

        // Toggle change
        const toggleInput = this.querySelector('.timer-toggle');
        if (toggleInput) {
            toggleInput.addEventListener('change', async (e) => {
                e.stopPropagation();
                const index = parseInt(e.target.dataset.index);
                currentConfig.Timers[index].IsActive = e.target.checked;
                await saveConfiguration();
            });
        }

        // Delete button
        const deleteBtn = this.querySelector('.btn-delete');
        if (deleteBtn) {
            deleteBtn.addEventListener('click', async (e) => {
                e.stopPropagation();
                const index = parseInt(e.currentTarget.dataset.index);
                await deleteTimer(index);
            });
        }
    }
}

class AlertMessage extends HTMLElement {
    constructor() {
        super();
    }

    connectedCallback() {
        const message = this.getAttribute('data-message');
        const type = this.getAttribute('data-type');

        // Use external template and CSS classes
        const template = document.getElementById('alert-component-template');
        const clone = template.content.cloneNode(true);

        // Apply CSS classes
        this.className = `alert alert-${type}`;

        // Set message content
        const alertDiv = clone.querySelector('.alert-message');
        alertDiv.textContent = message;

        this.appendChild(clone);
        document.body.appendChild(this);

        // Auto-remove after 3 seconds
        setTimeout(() => {
            this.style.opacity = '0';
            this.style.transform = 'translateX(-50%) translateY(-20px)';
            setTimeout(() => this.remove(), 300);
        }, 3000);
    }
}

// Register custom elements
customElements.define('timer-card', TimerCard);
customElements.define('alert-message', AlertMessage);

// Theme Management
function initializeTheme() {
    console.log('Initializing theme...');
    const savedTheme = localStorage.getItem('theme');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const initialTheme = savedTheme || (prefersDark ? 'dark' : 'light');

    console.log('Initial theme:', initialTheme);
    setTheme(initialTheme);

    const themeToggle = document.getElementById('theme-toggle-input');
    console.log('Theme toggle element:', themeToggle);

    if (themeToggle) {
        themeToggle.checked = initialTheme === 'dark';
        themeToggle.addEventListener('change', (e) => {
            const newTheme = e.target.checked ? 'dark' : 'light';
            console.log('Theme changed to:', newTheme);
            setTheme(newTheme);
            localStorage.setItem('theme', newTheme);
        });
    } else {
        console.error('Theme toggle input not found!');
    }

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
        if (!localStorage.getItem('theme')) {
            const newTheme = e.matches ? 'dark' : 'light';
            setTheme(newTheme);
            if (themeToggle) {
                themeToggle.checked = e.matches;
            }
        }
    });
}

function setTheme(theme) {
    console.log('Setting theme to:', theme);
    document.documentElement.setAttribute('data-theme', theme);
}

// Load configuration from API
async function loadConfiguration() {
    try {
        console.log('Loading configuration...');
        const response = await fetch('/api/config');
        console.log('Response status:', response.status);

        if (!response.ok) {
            throw new Error(`Failed to load configuration: ${response.status} ${response.statusText}`);
        }

        const text = await response.text();
        console.log('Response text:', text);

        currentConfig = JSON.parse(text);
        console.log('Loaded configuration:', currentConfig);

        renderTimers();
        hideLoading();
    } catch (error) {
        console.error('Error loading configuration:', error);
        showAlert('Failed to load timer configuration: ' + error.message, 'error');
        hideLoading();

        // Initialize with empty config if load fails
        currentConfig = { Timers: [] };
        renderTimers();
    }
}

// Save configuration to API
async function saveConfiguration() {
    try {
        const response = await fetch('/api/config', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(currentConfig)
        });

        if (!response.ok) throw new Error('Failed to save configuration');

        console.log('Saved configuration');
        showAlert('Configuration saved successfully', 'success');
    } catch (error) {
        console.error('Error saving configuration:', error);
        showAlert('Failed to save configuration', 'error');
    }
}

// Render timers list
function renderTimers() {
    const timerList = document.getElementById('timer-list');
    timerList.innerHTML = '';

    if (!currentConfig || !currentConfig.Timers || currentConfig.Timers.length === 0) {
        timerList.innerHTML = '<p class="empty-state">No timers configured. Click "Create" to add your first timer.</p>';
        return;
    }

    currentConfig.Timers.forEach((timer, index) => {
        const timerCard = document.createElement('timer-card');
        timerCard.setAttribute('data-timer', JSON.stringify(timer));
        timerCard.setAttribute('data-index', index);
        timerList.appendChild(timerCard);
    });
}



// Open modal for creating or editing timer
function openModal(timerIndex = null) {
    console.log('openModal called with timerIndex:', timerIndex);
    editingTimerIndex = timerIndex;
    const modal = document.getElementById('timer-modal');
    const title = document.getElementById('modal-title');

    if (timerIndex === null) {
        // Creating new timer
        console.log('Creating new timer');
        title.textContent = 'Create Timer';
        document.getElementById('modal-timer-name').value = '';
        setTimeValues(0, 5, 0);
        document.getElementById('modal-haptic').value = 'jingle';
    } else {
        // Editing existing timer
        console.log('Editing existing timer at index:', timerIndex);
        title.textContent = 'Edit Timer';
        const timer = currentConfig.Timers[timerIndex];
        console.log('Timer data:', timer);
        document.getElementById('modal-timer-name').value = timer.Name;
        setTimeValues(timer.Hours, timer.Minutes, timer.Seconds);
        document.getElementById('modal-haptic').value = timer.Haptic;
        console.log('Modal values set to:', {
            name: timer.Name,
            hours: timer.Hours,
            minutes: timer.Minutes,
            seconds: timer.Seconds,
            haptic: timer.Haptic
        });
    }

    modal.style.display = 'flex';
}// Set time values for both inputs and sliders
function setTimeValues(hours, minutes, seconds) {
    document.getElementById('modal-hours').value = hours;
    document.getElementById('modal-hours-slider').value = hours;
    document.getElementById('modal-minutes').value = minutes;
    document.getElementById('modal-minutes-slider').value = minutes;
    document.getElementById('modal-seconds').value = seconds;
    document.getElementById('modal-seconds-slider').value = seconds;
}

// Restore default timers without removing user-created ones
function restoreDefaults() {
    const defaultTimers = [
        {
            Id: "default-5min",
            Name: "Quick Break",
            Hours: 0,
            Minutes: 5,
            Seconds: 0,
            Haptic: "jingle",
            IsActive: true
        },
        {
            Id: "default-15min",
            Name: "Short Session",
            Hours: 0,
            Minutes: 15,
            Seconds: 0,
            Haptic: "knock",
            IsActive: true
        },
        {
            Id: "default-30min",
            Name: "Work Session",
            Hours: 0,
            Minutes: 30,
            Seconds: 0,
            Haptic: "ringing",
            IsActive: true
        },
        {
            Id: "default-1hour",
            Name: "Long Session",
            Hours: 1,
            Minutes: 0,
            Seconds: 0,
            Haptic: "jingle",
            IsActive: true
        }
    ];

    // Check which defaults are missing
    const existingIds = new Set(currentConfig.Timers.map(t => t.Id));
    const timersToAdd = defaultTimers.filter(timer => !existingIds.has(timer.Id));

    if (timersToAdd.length === 0) {
        showAlert('All default timers are already present', 'info');
        return;
    }

    // Add missing defaults
    currentConfig.Timers.push(...timersToAdd);
    saveConfiguration();
    renderTimers();
    showAlert(`Added ${timersToAdd.length} default timer(s)`, 'success');
}

// Close modal
function closeModal() {
    const modal = document.getElementById('timer-modal');
    modal.style.display = 'none';
    editingTimerIndex = null;
}

// Save modal (create or update timer)
function saveModal() {
    const name = document.getElementById('modal-timer-name').value.trim() || 'Untitled Timer';
    const hours = parseInt(document.getElementById('modal-hours').value) || 0;
    const minutes = parseInt(document.getElementById('modal-minutes').value) || 0;
    const seconds = parseInt(document.getElementById('modal-seconds').value) || 0;
    const haptic = document.getElementById('modal-haptic').value;

    // Validate duration
    if (hours === 0 && minutes === 0 && seconds === 0) {
        showAlert('Timer duration must be greater than 0', 'error');
        return;
    }

    const timer = {
        Id: editingTimerIndex === null ? generateId() : currentConfig.Timers[editingTimerIndex].Id,
        Name: name,
        Hours: hours,
        Minutes: minutes,
        Seconds: seconds,
        Haptic: haptic,
        IsActive: editingTimerIndex === null ? true : currentConfig.Timers[editingTimerIndex].IsActive
    };

    if (editingTimerIndex === null) {
        // Create new timer
        if (!currentConfig.Timers) {
            currentConfig.Timers = [];
        }
        currentConfig.Timers.push(timer);
    } else {
        // Update existing timer
        currentConfig.Timers[editingTimerIndex] = timer;
    }

    saveConfiguration();
    renderTimers();
    closeModal();
}

// Delete timer
function deleteTimer(index) {
    if (confirm('Are you sure you want to delete this timer?')) {
        currentConfig.Timers.splice(index, 1);
        saveConfiguration();
        renderTimers();
    }
}

// Generate unique ID
function generateId() {
    return 'timer_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
}

// Show/hide loading
function hideLoading() {
    const loading = document.getElementById('loading');
    const mainContent = document.getElementById('main-content');

    if (loading) loading.style.display = 'none';
    if (mainContent) mainContent.style.display = 'block';
}

// Show alert message
function showAlert(message, type) {
    const alert = document.createElement('alert-message');
    alert.setAttribute('data-message', message);
    alert.setAttribute('data-type', type);
}

// Setup slider and input synchronization
function setupSliderSync() {
    // Hours
    const hoursInput = document.getElementById('modal-hours');
    const hoursSlider = document.getElementById('modal-hours-slider');

    hoursInput.addEventListener('input', () => {
        hoursSlider.value = hoursInput.value;
    });

    hoursSlider.addEventListener('input', () => {
        hoursInput.value = hoursSlider.value;
    });

    // Minutes
    const minutesInput = document.getElementById('modal-minutes');
    const minutesSlider = document.getElementById('modal-minutes-slider');

    minutesInput.addEventListener('input', () => {
        minutesSlider.value = minutesInput.value;
    });

    minutesSlider.addEventListener('input', () => {
        minutesInput.value = minutesSlider.value;
    });

    // Seconds
    const secondsInput = document.getElementById('modal-seconds');
    const secondsSlider = document.getElementById('modal-seconds-slider');

    secondsInput.addEventListener('input', () => {
        secondsSlider.value = secondsInput.value;
    });

    secondsSlider.addEventListener('input', () => {
        secondsInput.value = secondsSlider.value;
    });
}

// Preview haptic feedback
async function previewHaptic(hapticName) {
    if (!hapticName || hapticName === 'none') {
        return;
    }

    try {
        await fetch('/api/haptic-preview', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ hapticName: hapticName })
        });
    } catch (error) {
        console.error('Error previewing haptic:', error);
    }
}

// Setup haptic preview on selection change
function setupHapticPreview() {
    const hapticSelect = document.getElementById('modal-haptic');
    if (hapticSelect) {
        hapticSelect.addEventListener('change', (e) => {
            previewHaptic(e.target.value);
        });
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM loaded, initializing...');
    initializeTheme();
    setupSliderSync();
    setupHapticPreview();
    loadConfiguration();
});

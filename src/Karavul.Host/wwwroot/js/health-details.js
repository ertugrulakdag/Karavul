function renderHealthDetails(container, json, labels) {
    container.replaceChildren();
    container.classList.add('health-details');

    function element(tag, text, className) {
        const node = document.createElement(tag);
        if (text !== undefined && text !== null) node.textContent = String(text);
        if (className) node.className = className;
        return node;
    }

    function isObject(value) {
        return value !== null && typeof value === 'object' && !Array.isArray(value);
    }

    function property(value, name) {
        if (!isObject(value)) return null;
        const key = Object.keys(value).find(key => key.toLowerCase() === name.toLowerCase());
        return key === undefined ? null : value[key];
    }

    function decoded(value) {
        if (typeof value !== 'string' || !/^[\s]*[\[{]/.test(value)) return value;
        try {
            const parsed = JSON.parse(value);
            return parsed !== null && typeof parsed === 'object' ? parsed : value;
        } catch {
            return value;
        }
    }

    function text(value) {
        if (value === null || value === undefined) return '—';
        return typeof value === 'object' ? JSON.stringify(value) : String(value);
    }

    function duration(value, msKey, durationKey) {
        const milliseconds = property(value, msKey);
        return milliseconds !== null && milliseconds !== ''
            ? text(milliseconds) + ' ms'
            : property(value, durationKey);
    }

    function normalizedStatus(value) {
        return String(value ?? '').trim().toLowerCase();
    }

    function status(value) {
        const node = element('span', text(value), 'health-status');
        const normalized = normalizedStatus(value);
        const color = normalized === 'healthy' ? 'green'
            : normalized === 'degraded' ? 'yellow'
            : normalized === 'unhealthy' ? 'red' : null;
        node.style.color = color ? 'var(--' + color + ')' : 'var(--text-muted)';
        node.dataset.state = color || 'unknown';
        return node;
    }

    function components(value) {
        const result = [];
        const checks = property(value, 'checks');
        if (Array.isArray(checks)) {
            checks.filter(isObject).forEach(check => result.push({ name: property(check, 'name'), value: check }));
        }
        const entries = property(value, 'entries');
        if (isObject(entries)) {
            Object.entries(entries).filter(([, entry]) => isObject(entry))
                .forEach(([name, entry]) => result.push({ name, value: entry }));
        }
        return result;
    }

    function isReport(value) {
        return isObject(value) && (Array.isArray(property(value, 'checks')) || isObject(property(value, 'entries'))
            || ['healthy', 'degraded', 'unhealthy'].includes(normalizedStatus(property(value, 'status'))));
    }

    function fieldLabel(key) {
        const known = {
            version: labels.version, environment: labels.environment, server: labels.server,
            assembly: labels.assembly, assemblies: labels.assemblies, description: labels.description,
            data: labels.data, exception: labels.exception, status: labels.status,
            durationms: labels.duration, totaldurationms: labels.totalDuration
        };
        return Object.hasOwn(known, key.toLowerCase()) ? known[key.toLowerCase()] || key : key;
    }

    function fields(parent, values) {
        if (values.length === 0) return;
        const list = element('dl', null, 'health-fields');
        values.forEach(([key, value]) => {
            const field = element('div', null, 'health-field');
            field.appendChild(element('dt', key));
            field.appendChild(element('dd', text(value)));
            list.appendChild(field);
        });
        parent.appendChild(list);
    }

    function group(parent, name, value, depth) {
        const details = element('details', null, 'health-data-group');
        const summary = element('summary');
        summary.appendChild(element('span', name, 'health-group-name'));
        if (isReport(value)) {
            if (property(value, 'status')) summary.appendChild(status(property(value, 'status')));
            details.open = depth < 3 || ['unhealthy', 'degraded'].includes(normalizedStatus(property(value, 'status')));
        } else {
            const count = Array.isArray(value) ? value.length : Object.keys(value).length;
            summary.appendChild(element('span', count + ' ' + (Array.isArray(value) ? labels.items : labels.fields), 'health-count'));
            details.open = depth < 3 && !Array.isArray(value) && count <= 6;
        }
        details.appendChild(summary);
        const content = element('div', null, 'health-group-content');
        renderValue(content, value, depth + 1, true);
        details.appendChild(content);
        parent.appendChild(details);
    }

    function renderValue(parent, original, depth, skipReportStatus = false) {
        const value = decoded(original);
        if (depth > 20) {
            parent.appendChild(element('pre', typeof value === 'object' ? JSON.stringify(value, null, 2) : text(value), 'health-raw'));
        } else if (isReport(value)) {
            renderReport(parent, value, depth, skipReportStatus);
        } else if (Array.isArray(value)) {
            if (value.length === 0) {
                parent.appendChild(element('span', labels.empty, 'health-empty'));
            } else if (value.every(item => decoded(item) === null || typeof decoded(item) !== 'object')) {
                const list = element('ul', null, 'health-value-list');
                value.forEach(item => list.appendChild(element('li', text(item))));
                parent.appendChild(list);
            } else {
                value.forEach((item, index) => {
                    const parsed = decoded(item);
                    if (parsed !== null && typeof parsed === 'object')
                        group(parent, text(property(parsed, 'name') ?? property(parsed, 'id') ?? '#' + (index + 1)), parsed, depth);
                    else fields(parent, [['#' + (index + 1), item]]);
                });
            }
        } else if (isObject(value)) {
            const simple = [];
            const nested = [];
            Object.entries(value).forEach(([key, item]) => {
                const parsed = decoded(item);
                if (parsed !== null && typeof parsed === 'object') nested.push([key, parsed]);
                else simple.push([fieldLabel(key), parsed]);
            });
            fields(parent, simple);
            nested.forEach(([key, item]) => group(parent, fieldLabel(key), item, depth));
            if (simple.length === 0 && nested.length === 0)
                parent.appendChild(element('span', labels.empty, 'health-empty'));
        } else {
            parent.appendChild(element('span', text(value), 'health-scalar'));
        }
    }

    function renderComponent(parent, component, depth) {
        const value = component.value;
        const details = element('details', null, 'health-component');
        const state = property(value, 'status');
        details.open = depth === 0 || ['unhealthy', 'degraded'].includes(normalizedStatus(state));
        const summary = element('summary');
        summary.appendChild(element('strong', text(component.name), 'health-component-name'));
        summary.appendChild(status(state));
        const elapsed = duration(value, 'durationMs', 'duration');
        if (elapsed !== null) summary.appendChild(element('span', elapsed, 'health-duration'));
        details.appendChild(summary);
        const body = element('div', null, 'health-component-body');

        const description = property(value, 'description');
        if (description) body.appendChild(element('p', text(description), 'health-description'));
        const tags = property(value, 'tags');
        if (Array.isArray(tags) && tags.length > 0) {
            const tagList = element('div', null, 'health-tags');
            tagList.appendChild(element('span', labels.tags + ':', 'health-label'));
            tags.forEach(tag => tagList.appendChild(element('span', text(tag), 'health-tag')));
            body.appendChild(tagList);
        }

        Object.entries(value).forEach(([key, extra]) => {
            if (['name', 'status', 'duration', 'durationms', 'description', 'tags'].includes(key.toLowerCase())
                || extra === null || extra === '' || (isObject(extra) && Object.keys(extra).length === 0)) return;
            const section = element('section', null, 'health-extra');
            section.appendChild(element('h3', fieldLabel(key), 'health-section-label'));
            renderValue(section, extra, depth + 1);
            body.appendChild(section);
        });

        if (!body.hasChildNodes()) body.appendChild(element('span', labels.noDetails, 'health-empty'));
        details.appendChild(body);
        parent.appendChild(details);
    }

    function renderReport(parent, report, depth, skipStatus = false) {
        const section = element('section', null, 'health-report');
        const summary = element('div', null, 'health-report-summary');
        if (!skipStatus && property(report, 'status')) {
            summary.appendChild(element('span', labels.status + ':', 'health-label'));
            summary.appendChild(status(property(report, 'status')));
        }
        const elapsed = duration(report, 'totalDurationMs', 'totalDuration');
        if (elapsed !== null && elapsed !== '') summary.appendChild(element('span', labels.totalDuration + ': ' + text(elapsed), 'health-duration'));
        if (summary.hasChildNodes()) section.appendChild(summary);

        const metadata = [];
        ['version', 'environment', 'server', 'assembly'].forEach(key => {
            const value = property(report, key);
            if (value !== null && value !== '') metadata.push([fieldLabel(key), value]);
        });
        fields(section, metadata);
        if (property(report, 'description')) section.appendChild(element('p', text(property(report, 'description')), 'health-description'));

        const reportComponents = components(report);
        reportComponents.forEach(component => renderComponent(section, component, depth));
        if (reportComponents.length === 0) {
            ['checks', 'entries'].forEach(key => {
                const value = property(report, key);
                if (value !== null && typeof value === 'object' && Object.keys(value).length > 0)
                    group(section, key, value, depth + 1);
            });
        }
        const known = ['status', 'totalduration', 'totaldurationms', 'version', 'environment', 'server', 'assembly', 'description', 'checks', 'entries'];
        Object.entries(report).forEach(([key, value]) => {
            if (known.includes(key.toLowerCase()) || value === null || value === '') return;
            const parsed = decoded(value);
            if (parsed !== null && typeof parsed === 'object') group(section, fieldLabel(key), parsed, depth + 1);
            else fields(section, [[fieldLabel(key), parsed]]);
        });
        parent.appendChild(section);
    }

    let data;
    try {
        data = JSON.parse(json);
    } catch {
        container.appendChild(element('pre', json, 'health-raw'));
        return;
    }
    renderValue(container, data, 0);
}
